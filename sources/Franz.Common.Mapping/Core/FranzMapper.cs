using Franz.Common.Errors;
using Franz.Common.Mapping.Abstractions;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Franz.Common.Mapping.Core;

public class FranzMapper(MappingConfiguration config) : IFranzMapper
{
  private readonly MappingConfiguration _config = config;

  // =========================================================
  // CACHES
  // =========================================================
  private static readonly ConcurrentDictionary<Type, PropertyInfo[]> AssignablePropsCache = new();
  private static readonly ConcurrentDictionary<Type, ConstructorInfo?> ConstructorCache = new();
  private static readonly ConcurrentDictionary<(Type, Type), Func<FranzMapper, object, MappingContext, object>> DelegateCache = new();

  // =========================================================
  // CONTEXT
  // =========================================================
  private sealed class MappingContext
  {
    public HashSet<object> Visited { get; } = new(ReferenceEqualityComparer.Instance);

    public void Enter(object obj)
    {
      if (obj == null) return;
      if (!Visited.Add(obj))
        throw new TechnicalException("Circular mapping detected.");
    }
  }

  // =========================================================
  // PUBLIC API
  // =========================================================
  public TDestination Map<TSource, TDestination>([DisallowNull] TSource source)
  {
    if (source == null)
      throw new ArgumentNullException(nameof(source));

    var ctx = new MappingContext();
    return MapInternal<TSource, TDestination>(source, ctx);
  }

  // =========================================================
  // CORE ENGINE
  // =========================================================
  private TDestination MapInternal<TSource, TDestination>(
      TSource source,
      MappingContext ctx)
  {
    ctx.Enter(source!);

    var srcType = typeof(TSource);
    var destType = typeof(TDestination);

    // =========================================================
    // 0. VALUE UNWRAP REWRITE STEP (CRITICAL FOR INLINE ROOT VALS)
    // =========================================================
    var valueProp = source!.GetType().GetProperty("Value");
    if (valueProp != null)
    {
      var inner = valueProp.GetValue(source);
      if (inner != null)
        return (TDestination)ResolveValue(inner.GetType(), destType, inner, ctx)!;
    }

    // =========================================================
    // 1. COLLECTIONS
    // =========================================================
    if (IsCollection(destType))
    {
      return (TDestination)MapCollection(srcType, destType, source!, ctx);
    }

    // =========================================================
    // 2. CONFIGURED MAPPING
    // =========================================================
    if (_config.TryGetMapping<TSource, TDestination>(out var expr))
    {
      var typed = expr!;

      // 2A. ConstructUsing (terminal)
      if (typed.Constructor is Func<TSource, TDestination> ctor)
        return ctor(source!);

      // 2B. Constructor binding
      var destination = CreateInstanceSmart(source!, typed, ctx);

      ApplyMapping(source!, destination, typed, ctx);

      return (TDestination)destination;
    }

    // =========================================================
    // 3. FALLBACK
    // =========================================================
    return DefaultMap<TSource, TDestination>(source!, ctx);
  }

  // =========================================================
  // CONSTRUCTOR ENGINE
  // =========================================================
  // =========================================================
  // CONSTRUCTOR ENGINE
  // =========================================================
  private object CreateInstanceSmart<TSource, TDestination>(
      TSource source,
      MappingExpression<TSource, TDestination> expr,
      MappingContext ctx)
  {
    var srcType = typeof(TSource);
    var destType = typeof(TDestination);

    var ctor = GetCtor(destType);

    if (ctor == null || ctor.GetParameters().Length == 0)
    {
      return Activator.CreateInstance(destType)
          ?? throw new TechnicalException(
              $"Cannot create instance of {destType.FullName}");
    }

    var parameters = ctor.GetParameters()
        .Select(parameter =>
        {
          var sourceMemberName =
              expr.MemberBindings.TryGetValue(parameter.Name!, out var mapped)
                  ? mapped
                  : parameter.Name;

          var sourceProperty = srcType.GetProperty(
              sourceMemberName!,
              BindingFlags.Public |
              BindingFlags.Instance |
              BindingFlags.IgnoreCase);

          if (sourceProperty == null)
          {
            if (expr.IsStrict)
            {
              throw new TechnicalException(
                  $"Missing constructor binding for parameter '{parameter.Name}' " +
                  $"when mapping {srcType.Name} -> {destType.Name}");
            }

            return GetDefault(parameter.ParameterType);
          }

          var rawValue = sourceProperty.GetValue(source);

          return ResolveValue(
                     sourceProperty.PropertyType,
                     parameter.ParameterType,
                     rawValue,
                     ctx)
                 ?? GetDefault(parameter.ParameterType);
        })
        .ToArray();

    return ctor.Invoke(parameters);
  }

  // =========================================================
  // PROPERTY MAPPING
  // =========================================================
  private void ApplyMapping<TSource, TDestination>(
      TSource source,
      object destination,
      MappingExpression<TSource, TDestination> expr,
      MappingContext ctx)
  {
    var srcType = typeof(TSource);
    var destType = destination.GetType();

    // Cache structural queries containing standard setters AND init-only properties
    var props = AssignablePropsCache.GetOrAdd(destType,
        t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
              .Where(p => p.CanWrite || IsInitOnly(p))
              .ToArray());

    var ctor = GetCtor(destType);
    var ctorParams = ctor?.GetParameters().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase)
                     ?? [];

    foreach (var destProp in props)
    {
      // Shielding: Skip fields that were already mutated during constructor invocation
      if (ctorParams.Contains(destProp.Name))
        continue;

      if (expr.IgnoredMembers.Contains(destProp.Name))
        continue;

      var srcName =
          expr.MemberBindings.TryGetValue(destProp.Name, out var mapped)
              ? mapped
              : destProp.Name;

      var srcProp = srcType.GetProperty(srcName,
          BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

      if (srcProp == null)
      {
        if (expr.IsStrict)
          throw new TechnicalException($"Missing mapping: {destProp.Name}");
        continue;
      }

      var value = ResolveValue(
          srcProp.PropertyType,
          destProp.PropertyType,
          srcProp.GetValue(source),
          ctx);

      destProp.SetValue(destination, value);
    }
  }

  // =========================================================
  // VALUE RESOLUTION
  // =========================================================
  private object? ResolveValue(
    Type srcType,
    Type destType,
    object? value,
    MappingContext ctx)
  {
    if (value == null)
      return null;

    var valueProp = value.GetType().GetProperty("Value");

    if (valueProp != null)
    {
      var inner = valueProp.GetValue(value);

      if (inner != null)
      {
        value = inner;
        srcType = inner.GetType();
      }
    }

    if (destType.IsInstanceOfType(value))
      return value;

    if (IsCollection(destType))
      return MapCollection(srcType, destType, value!, ctx);

    if (destType.IsAssignableFrom(srcType))
      return value;

    return InvokeMap(srcType, destType, value!, ctx);
  }

  // =========================================================
  // FAST DISPATCH (NO REFLECTION INVOKE)
  // =========================================================
  private object InvokeMap(Type srcType, Type destType, object value, MappingContext ctx)
  {
    var key = (srcType, destType);

    var del = DelegateCache.GetOrAdd(key, _ =>
        CreateDelegate(srcType, destType));

    return del(this, value, ctx);
  }

  private static Func<FranzMapper, object, MappingContext, object> CreateDelegate(Type src, Type dest)
  {
    var mapper = Expression.Parameter(typeof(FranzMapper), "m");
    var source = Expression.Parameter(typeof(object), "s");
    var ctx = Expression.Parameter(typeof(MappingContext), "c");

    var method = typeof(FranzMapper)
        .GetMethod(nameof(MapInternal), BindingFlags.NonPublic | BindingFlags.Instance)!
        .MakeGenericMethod(src, dest);

    var body = Expression.Call(
        mapper,
        method,
        Expression.Convert(source, src),
        ctx
    );

    return Expression.Lambda<Func<FranzMapper, object, MappingContext, object>>(
        Expression.Convert(body, typeof(object)),
        mapper, source, ctx
    ).Compile();
  }

  // =========================================================
  // COLLECTIONS
  // =========================================================
  private object MapCollection(Type srcType, Type destType, object source, MappingContext ctx)
  {
    var srcElem = GetElementType(srcType);
    var destElem = GetElementType(destType);

    var listType = typeof(List<>).MakeGenericType(destElem);
    var list = (IList)Activator.CreateInstance(listType)!;

    var del = DelegateCache.GetOrAdd((srcElem, destElem),
        _ => CreateDelegate(srcElem, destElem));

    foreach (var item in (IEnumerable)source)
    {
      list.Add(del(this, item!, ctx));
    }

    return list;
  }

  // =========================================================
  // FALLBACK
  // =========================================================
  private TDestination DefaultMap<TSource, TDestination>(TSource source, MappingContext ctx)
  {
    var srcType = typeof(TSource);
    var destType = typeof(TDestination);
    var ctor = GetCtor(destType);

    object dest;

    // Hardening: Handle parameter-heavy records inside unconfigured paths safely
    if (ctor != null && ctor.GetParameters().Length > 0)
    {
      var parameters = ctor.GetParameters().Select(p =>
      {
        var srcProp = srcType.GetProperty(p.Name!, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (srcProp == null) return GetDefault(p.ParameterType);

        return ResolveValue(srcProp.PropertyType, p.ParameterType, srcProp.GetValue(source), ctx) ?? GetDefault(p.ParameterType);
      }).ToArray();

      dest = ctor.Invoke(parameters);
    }
    else
    {
      dest = Activator.CreateInstance<TDestination>()!;
    }

    var ctorParams = ctor?.GetParameters().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
    var props = AssignablePropsCache.GetOrAdd(destType,
        t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
              .Where(p => p.CanWrite || IsInitOnly(p))
              .ToArray());

    foreach (var p in props)
    {
      if (ctorParams.Contains(p.Name)) continue;

      var src = srcType.GetProperty(p.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
      if (src == null) continue;

      var val = ResolveValue(src.PropertyType, p.PropertyType, src.GetValue(source), ctx);
      p.SetValue(dest, val);
    }

    return (TDestination)dest;
  }

  // =========================================================
  // HELPERS
  // =========================================================
  private static ConstructorInfo? GetCtor(Type t)
      => ConstructorCache.GetOrAdd(t,
          _ => t.GetConstructors()
                .OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault());

  private static bool IsCollection(Type t)
      => typeof(IEnumerable).IsAssignableFrom(t) && t != typeof(string);

  private static Type GetElementType(Type t)
      => t.IsArray ? t.GetElementType()! :
         t.IsGenericType ? t.GetGenericArguments()[0] :
         typeof(object);

  private static object? GetDefault(Type type)
      => type.IsValueType ? Activator.CreateInstance(type) : null;

  private static bool IsInitOnly(PropertyInfo propertyInfo)
  {
    var setMethod = propertyInfo.GetSetMethod(nonPublic: true);
    if (setMethod == null) return false;

    // Detects System.Runtime.CompilerServices.IsExternalInit runtime modifiers
    return setMethod.ReturnParameter.GetRequiredCustomModifiers()
        .Any(m => m.FullName == "System.Runtime.CompilerServices.IsExternalInit");
  }
}