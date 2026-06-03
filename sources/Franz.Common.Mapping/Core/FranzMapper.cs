using Franz.Common.Errors;
using Franz.Common.Mapping.Abstractions;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Franz.Common.Mapping.Core;

// =========================================================
// ATTRIBUTES
// =========================================================

/// <summary>
/// Marks a class or struct as a value object whose inner <c>Value</c> property
/// should be transparently unwrapped during mapping. Without this attribute,
/// a property named "Value" is treated as a regular property.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class ValueObjectAttribute : Attribute { }

/// <summary>
/// When a type has multiple constructors, marks the one FranzMapper should
/// prefer for construction. Without this attribute, the longest constructor wins.
/// </summary>
[AttributeUsage(AttributeTargets.Constructor)]
public sealed class MapConstructorAttribute : Attribute { }

// =========================================================
// MAPPER
// =========================================================

/// <summary>
/// Production-grade, zero-dependency object mapper for .NET.
///
/// Features:
///   • Immutable records (primary constructors)
///   • init-only properties
///   • Value object transparent unwrapping (opt-in via [ValueObject])
///   • Nested object graphs with circular-reference detection
///   • Collections: IEnumerable, List, arrays, IReadOnlyCollection,
///     IReadOnlyList, HashSet, ICollection
///   • Configured mappings: ConstructUsing, ForMember, Ignore, IsStrict
///   • Fallback convention-based mapping (name + case-insensitive)
///   • Compiled expression delegate cache (no repeated reflection invoke)
///   • Thread-safe static caches
/// </summary>
public sealed class FranzMapper(MappingConfiguration config) : IFranzMapper
{
  private readonly MappingConfiguration _config = config;

  // =========================================================
  // STATIC CACHES  (shared across all mapper instances)
  // =========================================================
  private static readonly ConcurrentDictionary<Type, PropertyInfo[]>
      AssignablePropsCache = new();

  private static readonly ConcurrentDictionary<Type, ConstructorInfo?>
      ConstructorCache = new();

  private static readonly ConcurrentDictionary<(Type Src, Type Dest),
      Func<FranzMapper, object, MappingContext, object>>
      DelegateCache = new();

  private static readonly ConcurrentDictionary<Type, bool>
      IsValueObjectCache = new();

  private static readonly ConcurrentDictionary<Type, PropertyInfo?>
      ValuePropertyCache = new();

  // =========================================================
  // MAPPING CONTEXT  (per-call, tracks visited references)
  // =========================================================
  private sealed class MappingContext
  {
    // Reference equality: safe for class instances, intentionally skipped
    // for value types (they cannot form reference cycles).
    private readonly HashSet<object> _visited =
        new(ReferenceEqualityComparer.Instance);

    public void Enter(object obj)
    {
      // Value types are stack-allocated copies; they cannot form cycles.
      if (obj.GetType().IsValueType) return;

      if (!_visited.Add(obj))
        throw new TechnicalException(
            $"[FranzMapper] Circular reference detected while mapping " +
            $"an instance of '{obj.GetType().FullName}'. " +
            $"Consider breaking the cycle or using ConstructUsing.");
    }
  }

  // =========================================================
  // PUBLIC API
  // =========================================================

  /// <summary>Maps <typeparamref name="TSource"/> to a new <typeparamref name="TDestination"/>.</summary>
  /// <exception cref="ArgumentNullException">When <paramref name="source"/> is null.</exception>
  /// <exception cref="TechnicalException">On mapping failure, missing binding (strict mode), or circular reference.</exception>
  public TDestination Map<TSource, TDestination>([DisallowNull] TSource source)
  {
    ArgumentNullException.ThrowIfNull(source);
    return MapInternal<TSource, TDestination>(source, new MappingContext());
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

    // Parameterless / no ctor found → plain Activator
    if (ctor == null || ctor.GetParameters().Length == 0)
    {
      return Activator.CreateInstance(destType)
          ?? throw new TechnicalException(
              $"[FranzMapper] Cannot create an instance of '{destType.FullName}'. " +
              $"Ensure it has a public constructor.");
    }

    var ctorArgs = ResolveConstructorArguments(
        source!, ctor, srcType, destType, expr.MemberBindings, expr.IsStrict, ctx);

    return ctor.Invoke(ctorArgs);
  }

  // Shared by both configured and fallback paths.
  private object?[] ResolveConstructorArguments(
      object source,
      ConstructorInfo ctor,
      Type srcType,
      Type destType,
      IReadOnlyDictionary<string, string>? memberBindings,
      bool isStrict,
      MappingContext ctx)
  {
    var parameters = ctor.GetParameters();
    var args = new object?[parameters.Length];

    for (var i = 0; i < parameters.Length; i++)
    {
      var param = parameters[i];

      var sourceName = memberBindings != null
          && memberBindings.TryGetValue(param.Name!, out var mapped)
          ? mapped
          : param.Name;

      var srcProp = srcType.GetProperty(
          sourceName!,
          BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

      if (srcProp == null)
      {
        if (isStrict)
          throw new TechnicalException(
              $"[FranzMapper] Strict mode: no source property found for " +
              $"constructor parameter '{param.Name}' " +
              $"when mapping {srcType.Name} → {destType.Name}.");

        args[i] = GetDefault(param.ParameterType);
        continue;
      }

      var raw = srcProp.GetValue(source);
      args[i] = ResolveValue(srcProp.PropertyType, param.ParameterType, raw, ctx)
                ?? GetDefault(param.ParameterType);
    }

    return args;
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