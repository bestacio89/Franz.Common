using System.Reflection;
using Franz.Common.Business.Domain;
using Franz.Common.Business.Domain.Factories;
using Franz.Common.Business.Repositories;
using Franz.Common.Mediator.Messages;

namespace Franz.Common.Architecture.Tests;

public sealed class FrameworkDomainContractTests
{
  [Fact]
  public void Entity_contract_exposes_read_only_identity_and_lifecycle_state()
  {
    var entityContract = typeof(IEntity<>);
    var keyType = entityContract.GetGenericArguments()[0];

    AssertReadOnlyProperty(entityContract, "Id", keyType);
    AssertReadOnlyProperty(entityContract, "IsDeleted", typeof(bool));
    AssertReadOnlyProperty(entityContract, "DateCreated", typeof(DateTimeOffset));

    AssertMethod(entityContract, "GetId", typeof(object));
    AssertMethod(entityContract, "MarkCreated", typeof(void), typeof(string));
    AssertMethod(entityContract, "MarkUpdated", typeof(void), typeof(string));
    AssertMethod(entityContract, "MarkDeleted", typeof(void), typeof(string));
  }

  [Fact]
  public void Entity_implementation_preserves_identity_ownership_and_interface_alignment()
  {
    var entityType = typeof(Entity<>);
    var keyType = entityType.GetGenericArguments()[0];
    var implementedContract = Assert.Single(entityType.GetInterfaces(), type =>
      type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEntity<>));

    Assert.Equal(keyType, Assert.Single(implementedContract.GetGenericArguments()));

    var identity = AssertProperty(entityType, "Id", keyType);
    Assert.True(identity.GetMethod?.IsPublic == true);
    Assert.True(identity.GetSetMethod(nonPublic: true)?.IsPrivate == true);

    foreach (var propertyName in new[]
    {
      "DateCreated",
      "LastModifiedDate",
      "CreatedBy",
      "LastModifiedBy",
      "IsDeleted",
      "DateDeleted",
      "DeletedBy"
    })
    {
      var property = entityType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
      Assert.NotNull(property);
      Assert.True(
        property!.GetSetMethod(nonPublic: true)?.IsPrivate == true,
        $"Entity lifecycle property '{propertyName}' must retain private setter ownership.");
    }

    Assert.All(
      entityType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
      constructor => Assert.True(
        constructor.IsFamily,
        "Entity construction must remain controlled by derived types and framework factories."));
  }

  [Fact]
  public void Entity_repository_contract_remains_bound_to_the_framework_entity_identity()
  {
    var repositoryParameters = typeof(IEntityRepository<,>).GetGenericArguments();
    var entityParameter = repositoryParameters[0];
    var keyParameter = repositoryParameters[1];
    var entityConstraint = Assert.Single(entityParameter.GetGenericParameterConstraints());

    Assert.True(entityConstraint.IsGenericType);
    Assert.Equal(typeof(Entity<>), entityConstraint.GetGenericTypeDefinition());
    Assert.Equal(keyParameter, Assert.Single(entityConstraint.GetGenericArguments()));

    AssertMethod(
      typeof(IEntityRepository<,>),
      "GetByIdAsync",
      typeof(Task<>).MakeGenericType(entityParameter),
      keyParameter,
      typeof(CancellationToken));
  }

  [Fact]
  public void Entity_factory_contract_remains_bound_to_framework_entities()
  {
    var factoryParameters = typeof(IEntityFactory<,>).GetGenericArguments();
    var keyParameter = factoryParameters[0];
    var entityParameter = factoryParameters[1];
    var entityConstraint = Assert.Single(entityParameter.GetGenericParameterConstraints());

    Assert.True(entityConstraint.IsGenericType);
    Assert.Equal(typeof(Entity<>), entityConstraint.GetGenericTypeDefinition());
    Assert.Equal(keyParameter, Assert.Single(entityConstraint.GetGenericArguments()));

    AssertMethod(typeof(IEntityFactory<,>), "Create", entityParameter);
  }

  [Fact]
  public void Aggregate_contract_preserves_event_and_identity_constraints()
  {
    var aggregateContract = typeof(IAggregateRoot<>);
    var eventParameter = Assert.Single(aggregateContract.GetGenericArguments());

    Assert.Contains(typeof(IEvent), eventParameter.GetGenericParameterConstraints());

    var aggregateImplementation = typeof(AggregateRoot<>);
    var implementationEventParameter = Assert.Single(aggregateImplementation.GetGenericArguments());
    var baseType = Assert.IsAssignableFrom<Type>(aggregateImplementation.BaseType);

    Assert.True(baseType.IsGenericType);
    Assert.Equal(typeof(Entity<>), baseType.GetGenericTypeDefinition());
    Assert.Equal(typeof(Guid), Assert.Single(baseType.GetGenericArguments()));
    Assert.Contains(typeof(IEvent), implementationEventParameter.GetGenericParameterConstraints());

    var implementedContract = Assert.Single(aggregateImplementation.GetInterfaces(), type =>
      type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IAggregateRoot<>));

    Assert.Equal(implementationEventParameter, Assert.Single(implementedContract.GetGenericArguments()));
  }

  [Fact]
  public void Generic_value_object_contract_preserves_self_type_and_equality_semantics()
  {
    var valueObjectType = typeof(ValueObject<>);
    var valueTypeParameter = Assert.Single(valueObjectType.GetGenericArguments());
    var selfConstraint = Assert.Single(valueTypeParameter.GetGenericParameterConstraints());

    Assert.True(selfConstraint.IsGenericType);
    Assert.Equal(typeof(ValueObject<>), selfConstraint.GetGenericTypeDefinition());
    Assert.Equal(valueTypeParameter, Assert.Single(selfConstraint.GetGenericArguments()));

    var equalityContract = Assert.Single(valueObjectType.GetInterfaces(), type =>
      type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEquatable<>));

    Assert.Equal(valueTypeParameter, Assert.Single(equalityContract.GetGenericArguments()));
  }

  private static void AssertReadOnlyProperty(Type contract, string name, Type propertyType)
  {
    var property = AssertProperty(contract, name, propertyType);
    Assert.NotNull(property.GetMethod);
    Assert.Null(property.SetMethod);
  }

  private static PropertyInfo AssertProperty(Type contract, string name, Type propertyType)
  {
    var property = contract.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);

    Assert.NotNull(property);
    Assert.Equal(propertyType, property!.PropertyType);

    return property;
  }

  private static void AssertMethod(
    Type contract,
    string name,
    Type returnType,
    params Type[] parameterTypes)
  {
    var method = contract
      .GetMethods(BindingFlags.Instance | BindingFlags.Public)
      .SingleOrDefault(candidate =>
        candidate.Name == name
        && candidate.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(parameterTypes));

    Assert.NotNull(method);
    Assert.Equal(returnType, method!.ReturnType);
  }
}
