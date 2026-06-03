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

    // ---------------------------------------------------------
    // 1. VALUE OBJECT UNWRAP  (opt-in via [ValueObject])
    //    Only fires when the SOURCE TYPE is decorated, not any
    //    type that happens to have a "Value" property.
    // ---------------------------------------------------------
    if (IsValueObject(source!.GetType()))
    {
      var inner = GetValueProperty(source.GetType())?.GetValue(source);
      if (inner != null)
        return (TDestination)ResolveValue(inner.GetType(), destType, inner, ctx)!;
    }

    // ---------------------------------------------------------
    // 2. COLLECTIONS
    // ---------------------------------------------------------
    if (IsCollection(destType))
      return (TDestination)MapCollection(srcType, destType, source!, ctx);

    // ---------------------------------------------------------
    // 3. CONFIGURED MAPPING
    // ---------------------------------------------------------
    if (_config.TryGetMapping<TSource, TDestination>(out var expr))
    {
      // 3A. ConstructUsing (terminal — caller owns the whole object)
      if (expr!.Constructor is Func<TSource, TDestination> ctor)
        return ctor(source!);

      // 3B. Constructor + property binding
      var destination = CreateInstanceSmart(source!, expr, ctx);
      ApplyMapping(source!, destination, expr, ctx);
      return (TDestination)destination;
    }

    // ---------------------------------------------------------
    // 4. CONVENTION FALLBACK
    // ---------------------------------------------------------
    return DefaultMap<TSource, TDestination>(source!, ctx);
  }

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

      // 1. explicit mapping override
      var sourceName =
          memberBindings != null &&
          memberBindings.TryGetValue(param.Name!, out var mapped)
              ? mapped
              : param.Name;

      // 2. find source property (case insensitive)
      var srcProp = srcType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
          .FirstOrDefault(p =>
              string.Equals(p.Name, sourceName, StringComparison.OrdinalIgnoreCase));

      // ❗ NEW FIX: fallback to "best match by type"
      if (srcProp == null)
      {
        srcProp = srcType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p =>
                p.PropertyType == param.ParameterType);
      }

      if (srcProp == null)
      {
        if (isStrict)
          throw new TechnicalException(
              $"[FranzMapper] Strict mode: no matching source property for ctor parameter '{param.Name}' " +
              $"on '{srcType.Name}' → '{destType.Name}'.");

        args[i] = GetDefault(param.ParameterType);
        continue;
      }

      var raw = srcProp.GetValue(source);

      args[i] = ResolveValue(
          srcProp.PropertyType,
          param.ParameterType,
          raw,
          ctx);
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

    var props = GetAssignableProps(destType);
    var ctor = GetCtor(destType);

    var ctorParams = BuildCtorParamSet(ctor);

    foreach (var destProp in props)
    {
      if (expr.IgnoredMembers.Contains(destProp.Name))
        continue;

      // Resolve source name (mapping or convention)
      var srcName = expr.MemberBindings != null &&
                    expr.MemberBindings.TryGetValue(destProp.Name, out var mapped)
          ? mapped
          : destProp.Name;

      var srcProp = srcType.GetProperty(
          srcName,
          BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

      if (srcProp == null)
      {
        if (expr.IsStrict)
          throw new TechnicalException(
              $"[FranzMapper] Strict mode: no source property '{srcName}' " +
              $"found for '{destProp.Name}'.");

        continue;
      }

      var raw = srcProp.GetValue(source);

      var value = ResolveValue(
          srcProp.PropertyType,
          destProp.PropertyType,
          raw,
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
    if (value == null) return null;

    // Unwrap value objects (opt-in only)
    if (IsValueObject(value.GetType()))
    {
      var inner = GetValueProperty(value.GetType())?.GetValue(value);
      if (inner != null)
      {
        value = inner;
        srcType = inner.GetType();
      }
    }

    // Already the right type — no work needed
    if (destType.IsInstanceOfType(value))
      return value;

    // Collection mapping
    if (IsCollection(destType))
      return MapCollection(srcType, destType, value, ctx);

    // Assignable without conversion
    if (destType.IsAssignableFrom(srcType))
      return value;

    // Recurse via compiled delegate
    return InvokeMap(srcType, destType, value, ctx);
  }

  // =========================================================
  // FAST DISPATCH  (compiled expression delegates, no MethodInfo.Invoke)
  // =========================================================
  private object InvokeMap(Type srcType, Type destType, object value, MappingContext ctx)
  {
    var del = DelegateCache.GetOrAdd(
        (srcType, destType),
        _ => CreateDelegate(srcType, destType));

    return del(this, value, ctx);
  }

  private static Func<FranzMapper, object, MappingContext, object>
      CreateDelegate(Type src, Type dest)
  {
    var mapperParam = Expression.Parameter(typeof(FranzMapper), "m");
    var sourceParam = Expression.Parameter(typeof(object), "s");
    var ctxParam = Expression.Parameter(typeof(MappingContext), "c");

    var method = typeof(FranzMapper)
        .GetMethod(nameof(MapInternal), BindingFlags.NonPublic | BindingFlags.Instance)!
        .MakeGenericMethod(src, dest);

    var call = Expression.Call(
        mapperParam,
        method,
        Expression.Convert(sourceParam, src),
        ctxParam);

    return Expression.Lambda<Func<FranzMapper, object, MappingContext, object>>(
        Expression.Convert(call, typeof(object)),
        mapperParam, sourceParam, ctxParam
    ).Compile();
  }

  // =========================================================
  // COLLECTION MAPPING
  // =========================================================
  private object MapCollection(
      Type srcType,
      Type destType,
      object source,
      MappingContext ctx)
  {
    if (source is not IEnumerable enumerable)
      throw new TechnicalException(
          $"[FranzMapper] Cannot map '{srcType.FullName}' as a collection: " +
          $"it does not implement IEnumerable.");

    var srcElem = GetElementType(srcType);
    var destElem = GetElementType(destType);

    // Build into List<TDest> as the universal intermediate
    var listType = typeof(List<>).MakeGenericType(destElem);
    var list = (IList)Activator.CreateInstance(listType)!;

    var del = DelegateCache.GetOrAdd(
        (srcElem, destElem),
        _ => CreateDelegate(srcElem, destElem));

    foreach (var item in enumerable)
    {
      if (item == null)
      {
        list.Add(null);
        continue;
      }
      list.Add(del(this, item, ctx));
    }

    return CoerceCollection(list, destType, destElem);
  }

  /// <summary>
  /// Converts the intermediate List&lt;T&gt; into whatever concrete collection
  /// type the destination property actually expects.
  /// </summary>
  private static object CoerceCollection(IList list, Type destType, Type elemType)
  {
    // T[]
    if (destType.IsArray)
    {
      var arr = Array.CreateInstance(elemType, list.Count);
      list.CopyTo(arr, 0);
      return arr;
    }

    var def = destType.IsGenericType ? destType.GetGenericTypeDefinition() : null;

    // HashSet<T>
    if (def == typeof(HashSet<>))
    {
      var setType = typeof(HashSet<>).MakeGenericType(elemType);
      var set = Activator.CreateInstance(setType)!;
      var addMeth = setType.GetMethod("Add")!;
      foreach (var item in list) addMeth.Invoke(set, [item]);
      return set;
    }

    // IReadOnlyList<T>, IReadOnlyCollection<T>, ICollection<T>,
    // IList<T>, IEnumerable<T>, List<T>  → List<T> satisfies all of these
    return list;
  }

  // =========================================================
  // CONVENTION FALLBACK
  // =========================================================
  private TDestination DefaultMap<TSource, TDestination>(TSource source, MappingContext ctx)
  {
    var srcType = typeof(TSource);
    var destType = typeof(TDestination);
    var ctor = GetCtor(destType);

    object dest;

    if (ctor != null && ctor.GetParameters().Length > 0)
    {
      var args = ResolveConstructorArguments(
          source!, ctor, srcType, destType,
          memberBindings: null,
          isStrict: false,
          ctx);
      dest = ctor.Invoke(args);
    }
    else
    {
      dest = Activator.CreateInstance<TDestination>()
          ?? throw new TechnicalException(
              $"[FranzMapper] Cannot create instance of '{destType.FullName}'.");
    }

    var ctorParams = BuildCtorParamSet(ctor);
    var props = GetAssignableProps(destType);

    foreach (var destProp in props)
    {
      if (ctorParams.Contains(destProp.Name)) continue;
      if (IsInitOnly(destProp)) continue; // can't set after ctor

      var srcProp = srcType.GetProperty(
          destProp.Name,
          BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

      if (srcProp == null) continue;

      var val = ResolveValue(
          srcProp.PropertyType,
          destProp.PropertyType,
          srcProp.GetValue(source),
          ctx);

      destProp.SetValue(dest, val);
    }

    return (TDestination)dest;
  }

  // =========================================================
  // HELPERS
  // =========================================================

  /// <summary>
  /// Prefers a constructor marked with [MapConstructor]; otherwise picks
  /// the public constructor with the most parameters (primary ctor convention).
  /// </summary>
  private static ConstructorInfo? GetCtor(Type type)
  {
    return ConstructorCache.GetOrAdd(type, static t =>
    {
      var ctors = t.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

      if (ctors.Length == 0)
        return null;

      // 1. Explicit priority: [MapConstructor]
      var attributed = ctors
          .FirstOrDefault(c => c.IsDefined(typeof(MapConstructorAttribute), false));

      if (attributed != null)
        return attributed;

      // 2. Prefer constructor with MOST parameters BUT ALSO:
      //    prefer one where parameter names match source properties better later
      //    (we do NOT hard bind here, just structural preference)

      return ctors
          .OrderByDescending(c => c.GetParameters().Length)
          .ThenBy(c => c.IsPublic ? 0 : 1)
          .First();
    });
  }
  private static HashSet<string> BuildCtorParamSet(ConstructorInfo? ctor)
      => ctor?.GetParameters()
             .Select(p => p.Name!)
             .ToHashSet(StringComparer.OrdinalIgnoreCase)
         ?? [];

  private static PropertyInfo[] GetAssignableProps(Type t)
      => AssignablePropsCache.GetOrAdd(t, static type =>
          type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
              .Where(p => p.CanWrite || IsInitOnly(p))
              .ToArray());

  /// <summary>
  /// Returns true only when the type itself is decorated with [ValueObject].
  /// This prevents accidental unwrapping of types like Task&lt;T&gt; or KeyValuePair
  /// that happen to expose a "Value" property.
  /// </summary>
  private static bool IsValueObject(Type t)
      => IsValueObjectCache.GetOrAdd(t,
          static type => type.IsDefined(typeof(ValueObjectAttribute), inherit: false));

  private static PropertyInfo? GetValueProperty(Type t)
      => ValuePropertyCache.GetOrAdd(t,
          static type => type.GetProperty("Value",
              BindingFlags.Public | BindingFlags.Instance));

  private static bool IsCollection(Type t)
      => t != typeof(string) && typeof(IEnumerable).IsAssignableFrom(t);

  private static Type GetElementType(Type t)
  {
    if (t.IsArray)
      return t.GetElementType()!;

    if (!t.IsGenericType)
      return typeof(object);

    var args = t.GetGenericArguments();

    // IDictionary<K,V> / Dictionary<K,V> → element is KeyValuePair<K,V>
    var def = t.GetGenericTypeDefinition();
    if (def == typeof(IDictionary<,>) || def == typeof(Dictionary<,>))
      return typeof(KeyValuePair<,>).MakeGenericType(args[0], args[1]);

    return args[0];
  }

  private static object? GetDefault(Type type)
      => type.IsValueType ? Activator.CreateInstance(type) : null;

  /// <summary>
  /// Detects C# init-only setters by checking for the
  /// <c>System.Runtime.CompilerServices.IsExternalInit</c> required modifier.
  /// </summary>
  private static bool IsInitOnly(PropertyInfo p)
  {
    var setter = p.GetSetMethod(nonPublic: true);
    if (setter == null) return false;
    return setter.ReturnParameter
                 .GetRequiredCustomModifiers()
                 .Any(static m =>
                     m.FullName ==
                     "System.Runtime.CompilerServices.IsExternalInit");
  }
}
