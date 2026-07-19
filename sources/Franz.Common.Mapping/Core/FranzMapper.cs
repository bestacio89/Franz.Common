using Franz.Common.Errors;
using Franz.Common.Mapping.Abstractions;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Franz.Common.Mapping.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class ValueObjectAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Constructor)]
public sealed class MapConstructorAttribute : Attribute
{
}


public sealed class FranzMapper(MappingConfiguration config) : IFranzMapper
{
  private readonly MappingConfiguration _config = config;


  private static readonly ConcurrentDictionary<Type, PropertyInfo[]>
      AssignablePropsCache = new();


  private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>>
      PropertyLookupCache = new();


  private static readonly ConcurrentDictionary<Type, ConstructorInfo?>
      ConstructorCache = new();


  private static readonly ConcurrentDictionary<(Type Src, Type Dest),
      Func<FranzMapper, object, MappingContext, object>>
      MappingPlanCache = new();


  private static readonly ConcurrentDictionary<Type, bool>
      IsValueObjectCache = new();


  private static readonly ConcurrentDictionary<Type, PropertyInfo?>
      ValuePropertyCache = new();


  private sealed class MappingContext
  {
    private readonly HashSet<object> _visited =
        new(ReferenceEqualityComparer.Instance);


    public void Enter(object obj)
    {
      if (obj.GetType().IsValueType)
        return;

      if (!_visited.Add(obj))
      {
        throw new TechnicalException(
            $"[FranzMapper] Circular reference detected while mapping " +
            $"'{obj.GetType().FullName}'.");
      }
    }
  }


  public TDestination Map<TSource, TDestination>(
      [DisallowNull] TSource source)
  {
    ArgumentNullException.ThrowIfNull(source);

    return MapInternal<TSource, TDestination>(
        source,
        new MappingContext());
  }


  private TDestination MapInternal<TSource, TDestination>(
      TSource source,
      MappingContext ctx)
  {
    ctx.Enter(source!);

    var srcType = typeof(TSource);
    var destType = typeof(TDestination);


    if (IsValueObject(source!.GetType()))
    {
      var inner = GetValueProperty(source.GetType())
          ?.GetValue(source);

      if (inner != null)
      {
        return (TDestination)ResolveValue(
            inner.GetType(),
            destType,
            inner,
            ctx)!;
      }
    }


    if (IsCollection(destType))
    {
      return (TDestination)MapCollection(
          srcType,
          destType,
          source!,
          ctx);
    }


    if (_config.TryGetMapping<TSource, TDestination>(out var expr))
    {
      if (expr!.Constructor is Func<TSource, TDestination> ctor)
      {
        return ctor(source!);
      }


      if (expr.Constructor is Func<TSource, IFranzMapper, TDestination> ctorWithMapper)
      {
        return ctorWithMapper(source!, this);
      }


      var destination = CreateInstanceSmart(
          source!,
          expr,
          ctx);


      ApplyMapping(
          source!,
          destination,
          expr,
          ctx);


      return (TDestination)destination;
    }


    return DefaultMap<TSource, TDestination>(
        source!,
        ctx);
  }


  private object CreateInstanceSmart<TSource, TDestination>(
      TSource source,
      MappingExpression<TSource, TDestination> expr,
      MappingContext ctx)
  {
    var destType = typeof(TDestination);
    var ctor = GetCtor(destType);


    if (ctor == null || ctor.GetParameters().Length == 0)
    {
      return Activator.CreateInstance(destType)
          ?? throw new TechnicalException(
              $"[FranzMapper] Cannot create '{destType.FullName}'.");
    }


    var args = ResolveConstructorArguments(
        source!,
        ctor,
        typeof(TSource),
        destType,
        expr.MemberBindings,
        expr.IsStrict,
        ctx);


    return ctor.Invoke(args);
  }


  private object?[] ResolveConstructorArguments(
      object source,
      ConstructorInfo ctor,
      Type srcType,
      Type destType,
      IReadOnlyDictionary<string, string>? bindings,
      bool strict,
      MappingContext ctx)
  {
    var parameters = ctor.GetParameters();
    var args = new object?[parameters.Length];

    var properties = GetPropertyLookup(srcType);


    for (var i = 0; i < parameters.Length; i++)
    {
      var parameter = parameters[i];


      var sourceName =
          bindings != null &&
          bindings.TryGetValue(parameter.Name!, out var mapped)
              ? mapped
              : parameter.Name!;


      properties.TryGetValue(
          sourceName,
          out var sourceProperty);


      if (sourceProperty == null)
      {
        sourceProperty = properties.Values
            .FirstOrDefault(p =>
                p.PropertyType == parameter.ParameterType);
      }


      if (sourceProperty == null)
      {
        if (strict)
        {
          throw new TechnicalException(
              $"[FranzMapper] Strict mode: missing constructor parameter '{parameter.Name}' " +
              $"for '{srcType.Name}' -> '{destType.Name}'.");
        }

        args[i] = GetDefault(parameter.ParameterType);
        continue;
      }


      args[i] = ResolveValue(
          sourceProperty.PropertyType,
          parameter.ParameterType,
          sourceProperty.GetValue(source),
          ctx);
    }


    return args;
  }


  private void ApplyMapping<TSource, TDestination>(
      TSource source,
      object destination,
      MappingExpression<TSource, TDestination> expr,
      MappingContext ctx)
  {
    var srcType = typeof(TSource);
    var destType = destination.GetType();

    var sourceProperties = GetPropertyLookup(srcType);

    var ctor = GetCtor(destType);

    var ctorParameters = BuildCtorParamSet(ctor);


    foreach (var destProp in GetAssignableProps(destType))
    {
      if (ctorParameters.Contains(destProp.Name))
        continue;


      if (expr.IgnoredMembers.Contains(destProp.Name))
        continue;


      var sourceName =
          expr.MemberBindings.TryGetValue(
              destProp.Name,
              out var mapped)
              ? mapped
              : destProp.Name;


      if (!sourceProperties.TryGetValue(
              sourceName,
              out var sourceProp))
      {
        if (expr.IsStrict)
        {
          throw new TechnicalException(
              $"[FranzMapper] Strict mode: missing source property '{sourceName}'.");
        }

        continue;
      }


      var value = ResolveValue(
          sourceProp.PropertyType,
          destProp.PropertyType,
          sourceProp.GetValue(source),
          ctx);


      destProp.SetValue(destination, value);
    }
  }
  private object? ResolveValue(
    Type srcType,
    Type destType,
    object? value,
    MappingContext ctx)
  {
    if (value == null)
      return null;


    if (IsValueObject(value.GetType()))
    {
      var inner = GetValueProperty(value.GetType())
          ?.GetValue(value);

      if (inner != null)
      {
        value = inner;
        srcType = inner.GetType();
      }
    }


    if (destType.IsInstanceOfType(value))
      return value;


    if (IsCollection(destType))
    {
      return MapCollection(
          srcType,
          destType,
          value,
          ctx);
    }


    if (destType.IsAssignableFrom(srcType))
      return value;


    return InvokeMap(
        srcType,
        destType,
        value,
        ctx);
  }



  private object InvokeMap(
      Type srcType,
      Type destType,
      object value,
      MappingContext ctx)
  {
    var plan = MappingPlanCache.GetOrAdd(
        (srcType, destType),
        static pair => CreateMappingPlan(
            pair.Src,
            pair.Dest));


    return plan(
        this,
        value,
        ctx);
  }



  private static Func<FranzMapper, object, MappingContext, object>
      CreateMappingPlan(
          Type source,
          Type destination)
  {
    var mapper = Expression.Parameter(
        typeof(FranzMapper),
        "mapper");

    var value = Expression.Parameter(
        typeof(object),
        "value");

    var context = Expression.Parameter(
        typeof(MappingContext),
        "context");


    var method =
        typeof(FranzMapper)
            .GetMethod(
                nameof(MapInternal),
                BindingFlags.Instance |
                BindingFlags.NonPublic)!
            .MakeGenericMethod(
                source,
                destination);


    var call = Expression.Call(
        mapper,
        method,
        Expression.Convert(value, source),
        context);


    return Expression.Lambda<
        Func<FranzMapper, object, MappingContext, object>>(
            Expression.Convert(
                call,
                typeof(object)),
            mapper,
            value,
            context)
        .Compile();
  }



  private object MapCollection(
      Type sourceType,
      Type destinationType,
      object source,
      MappingContext ctx)
  {
    if (source is not IEnumerable enumerable)
    {
      throw new TechnicalException(
          $"[FranzMapper] '{sourceType.FullName}' is not enumerable.");
    }


    var sourceElement = GetElementType(sourceType);
    var destinationElement = GetElementType(destinationType);


    var listType =
        typeof(List<>)
            .MakeGenericType(destinationElement);


    var list =
        (IList)Activator.CreateInstance(listType)!;


    var mapper =
        MappingPlanCache.GetOrAdd(
            (sourceElement, destinationElement),
            static pair => CreateMappingPlan(
                pair.Src,
                pair.Dest));


    foreach (var item in enumerable)
    {
      if (item == null)
      {
        list.Add(null);
        continue;
      }


      list.Add(
          mapper(
              this,
              item,
              ctx));
    }


    return CoerceCollection(
        list,
        destinationType,
        destinationElement);
  }



  private static object CoerceCollection(
      IList list,
      Type destinationType,
      Type elementType)
  {
    if (destinationType.IsArray)
    {
      var array =
          Array.CreateInstance(
              elementType,
              list.Count);

      list.CopyTo(array, 0);

      return array;
    }


    if (destinationType.IsGenericType &&
        destinationType.GetGenericTypeDefinition() ==
        typeof(HashSet<>))
    {
      var setType =
          typeof(HashSet<>)
              .MakeGenericType(elementType);


      var set =
          Activator.CreateInstance(setType)!;


      var add =
          setType.GetMethod("Add")!;


      foreach (var item in list)
      {
        add.Invoke(set, [item]);
      }


      return set;
    }


    return list;
  }



  private TDestination DefaultMap<TSource, TDestination>(
      TSource source,
      MappingContext ctx)
  {
    var destinationType = typeof(TDestination);
    var constructor = GetCtor(destinationType);


    object destination;


    if (constructor != null &&
        constructor.GetParameters().Length > 0)
    {
      destination =
          constructor.Invoke(
              ResolveConstructorArguments(
                  source!,
                  constructor,
                  typeof(TSource),
                  destinationType,
                  null,
                  false,
                  ctx));
    }
    else
    {
      destination =
          Activator.CreateInstance<TDestination>()
          ?? throw new TechnicalException(
              $"[FranzMapper] Cannot create '{destinationType.FullName}'.");
    }


    var ctorParameters =
        BuildCtorParamSet(constructor);


    var sourceProperties =
        GetPropertyLookup(typeof(TSource));


    foreach (var property in GetAssignableProps(destinationType))
    {
      if (ctorParameters.Contains(property.Name))
        continue;


      if (IsInitOnly(property))
        continue;


      if (!sourceProperties.TryGetValue(
              property.Name,
              out var sourceProperty))
      {
        continue;
      }


      property.SetValue(
          destination,
          ResolveValue(
              sourceProperty.PropertyType,
              property.PropertyType,
              sourceProperty.GetValue(source),
              ctx));
    }


    return (TDestination)destination;
  }



  private static ConstructorInfo? GetCtor(Type type)
  {
    return ConstructorCache.GetOrAdd(
        type,
        static t =>
        {
          var constructors =
              t.GetConstructors(
                  BindingFlags.Public |
                  BindingFlags.Instance);


          if (constructors.Length == 0)
            return null;


          var attributed =
              constructors.FirstOrDefault(
                  c => c.IsDefined(
                      typeof(MapConstructorAttribute),
                      false));


          if (attributed != null)
            return attributed;


          return constructors
              .OrderByDescending(
                  c => c.GetParameters().Length)
              .First();
        });
  }



  private static PropertyInfo[] GetAssignableProps(Type type)
  {
    return AssignablePropsCache.GetOrAdd(
        type,
        static t =>
            t.GetProperties(
                    BindingFlags.Public |
                    BindingFlags.Instance)
             .Where(p =>
                 p.CanWrite ||
                 IsInitOnly(p))
             .ToArray());
  }



  private static Dictionary<string, PropertyInfo>
      GetPropertyLookup(Type type)
  {
    return PropertyLookupCache.GetOrAdd(
        type,
        static t =>
            t.GetProperties(
                    BindingFlags.Public |
                    BindingFlags.Instance)
             .ToDictionary(
                 p => p.Name,
                 StringComparer.OrdinalIgnoreCase));
  }



  private static HashSet<string> BuildCtorParamSet(
      ConstructorInfo? ctor)
  {
    return ctor?
        .GetParameters()
        .Select(p => p.Name!)
        .ToHashSet(
            StringComparer.OrdinalIgnoreCase)
        ?? [];
  }



  private static bool IsValueObject(Type type)
  {
    return IsValueObjectCache.GetOrAdd(
        type,
        static t =>
            t.IsDefined(
                typeof(ValueObjectAttribute),
                false));
  }



  private static PropertyInfo? GetValueProperty(Type type)
  {
    return ValuePropertyCache.GetOrAdd(
        type,
        static t =>
            t.GetProperty(
                "Value",
                BindingFlags.Public |
                BindingFlags.Instance));
  }



  private static bool IsCollection(Type type)
  {
    return type != typeof(string) &&
           typeof(IEnumerable)
               .IsAssignableFrom(type);
  }



  private static Type GetElementType(Type type)
  {
    if (type.IsArray)
      return type.GetElementType()!;


    if (!type.IsGenericType)
      return typeof(object);


    return type.GetGenericArguments()[0];
  }



  private static object? GetDefault(Type type)
  {
    return type.IsValueType
        ? Activator.CreateInstance(type)
        : null;
  }



  private static bool IsInitOnly(PropertyInfo property)
  {
    var setter =
        property.GetSetMethod(
            nonPublic: true);


    if (setter == null)
      return false;


    return setter.ReturnParameter
        .GetRequiredCustomModifiers()
        .Any(
            static modifier =>
                modifier.FullName ==
                "System.Runtime.CompilerServices.IsExternalInit");
  }
}