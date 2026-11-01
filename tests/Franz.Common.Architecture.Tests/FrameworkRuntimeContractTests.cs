using Franz.Common.DependencyInjection;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Extensions;
using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Hosting;
using Franz.Common.Messaging.Storage;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Franz.Common.Architecture.Tests;

public sealed class FrameworkRuntimeContractTests
{
  [Fact]
  public void Mediator_core_registration_uses_a_scoped_dispatcher_and_is_idempotent()
  {
    var services = new ServiceCollection();

    services.AddFranzMediator();
    services.AddFranzMediator();

    var registrations = services
      .Where(descriptor => descriptor.ServiceType == typeof(IDispatcher))
      .ToArray();

    var registration = Assert.Single(registrations);
    Assert.Equal(ServiceLifetime.Scoped, registration.Lifetime);
    Assert.Equal(typeof(FranzDispatcher), registration.ImplementationType);
  }

  [Fact]
  public void Mediator_dispatch_contracts_accept_optional_cancellation_tokens()
  {
    var dispatcher = typeof(IDispatcher);
    var methods = dispatcher.GetMethods(BindingFlags.Public | BindingFlags.Instance);

    Assert.NotEmpty(methods);
    Assert.All(methods, method =>
    {
      var cancellationParameter = Assert.Single(method.GetParameters(), parameter =>
        parameter.ParameterType == typeof(CancellationToken));

      Assert.True(cancellationParameter.IsOptional);
    });
  }

  [Fact]
  public void Unit_of_work_contract_exposes_explicit_transaction_lifecycle_with_cancellation()
  {
    AssertAsyncCancellationMethod(typeof(IUnitOfWork), "BeginAsync");
    AssertAsyncCancellationMethod(typeof(IUnitOfWork), "CommitAsync");
    AssertAsyncCancellationMethod(typeof(IUnitOfWork), "RollbackAsync");
  }

  [Fact]
  public void Messaging_contracts_preserve_async_cancellation_and_resource_ownership()
  {
    AssertAsyncCancellationMethod(typeof(IMessagingSender), "SendAsync");
    AssertAsyncCancellationMethod(typeof(IMessagingPublisher), "Publish");
    AssertAsyncCancellationMethod(typeof(IListener), "Listen");
    AssertAsyncCancellationMethod(typeof(IListener), "StopListenAsync");
    AssertAsyncCancellationMethod(typeof(IMessageStore), "SaveAsync");
    AssertAsyncCancellationMethod(typeof(IMessageStore), "GetPendingAsync");
    AssertAsyncCancellationMethod(typeof(IMessageStore), "MarkAsSentAsync");
    AssertAsyncCancellationMethod(typeof(IMessageStore), "UpdateRetryAsync");
    AssertAsyncCancellationMethod(typeof(IMessageStore), "MoveToDeadLetterAsync");
    AssertAsyncCancellationMethod(typeof(IInboxStore), "HasProcessedAsync");
    AssertAsyncCancellationMethod(typeof(IInboxStore), "MarkProcessedAsync");

    Assert.Contains(typeof(IDisposable), typeof(IMessagingSender).GetInterfaces());
    Assert.Contains(typeof(IAsyncDisposable), typeof(IMessagingSender).GetInterfaces());
  }

  [Fact]
  public void Dependency_registration_strategy_is_idempotent_for_matching_pairs()
  {
    var services = new ServiceCollection();
    var strategy = new RegistrationStrategySkipExistingPair();
    var descriptor = new ServiceDescriptor(typeof(IContract), typeof(ContractImplementation), ServiceLifetime.Scoped);

    strategy.Apply(services, descriptor);
    strategy.Apply(services, descriptor);

    var registration = Assert.Single(services);
    Assert.Equal(typeof(IContract), registration.ServiceType);
    Assert.Equal(typeof(ContractImplementation), registration.ImplementationType);
    Assert.Equal(ServiceLifetime.Scoped, registration.Lifetime);
  }

  [Fact]
  public void Dependency_registration_strategy_rejects_lifetime_conflicts()
  {
    var services = new ServiceCollection();
    services.AddSingleton<IContract, ContractImplementation>();
    var strategy = new RegistrationStrategySkipExistingPair();
    var conflictingDescriptor = new ServiceDescriptor(typeof(IContract), typeof(ContractImplementation), ServiceLifetime.Scoped);

    Assert.Throws<InvalidOperationException>(() => strategy.Apply(services, conflictingDescriptor));

    var registration = Assert.Single(services);
    Assert.Equal(ServiceLifetime.Singleton, registration.Lifetime);
  }

  [Fact]
  public void Tenant_execution_context_rejects_invalid_or_inactive_tenants()
  {
    var validTenant = new TenantInfo(Guid.NewGuid(), "active");
    var suspendedTenant = new TenantInfo(Guid.NewGuid(), "suspended")
    {
      Status = TenantStatus.Suspended
    };

    Assert.Throws<ArgumentException>(() => new TenantInfo(Guid.Empty, "invalid"));
    Assert.Throws<ArgumentException>(() => CreateExecutionContext(suspendedTenant));

    var context = CreateExecutionContext(validTenant);
    Assert.Equal(validTenant.Id, context.TenantId);
    Assert.Same(validTenant, context.Tenant);
    Assert.Equal(TenantStatus.Active, context.Tenant.Status);
  }

  [Fact]
  public void Tenant_execution_context_exposes_stable_identity_as_read_only_properties()
  {
    var contextType = typeof(TenantExecutionContext);

    AssertReadOnlyProperty(contextType, nameof(TenantExecutionContext.Tenant), typeof(TenantInfo));
    AssertReadOnlyProperty(contextType, nameof(TenantExecutionContext.TenantId), typeof(Guid));
    AssertReadOnlyProperty(contextType, nameof(TenantExecutionContext.DomainId), typeof(Guid?));
    AssertReadOnlyProperty(contextType, nameof(TenantExecutionContext.Source), typeof(TenantResolutionSource));
    AssertReadOnlyProperty(contextType, nameof(TenantExecutionContext.ResolvedAt), typeof(DateTimeOffset));
    Assert.True(contextType.IsSealed);
  }

  private static TenantExecutionContext CreateExecutionContext(TenantInfo tenant)
    => new(tenant, null, TenantResolutionSource.Unknown, DateTimeOffset.UtcNow);

  private static void AssertAsyncCancellationMethod(Type contract, string methodName)
  {
    var methods = contract.GetMethods(BindingFlags.Public | BindingFlags.Instance)
      .Where(method => method.Name == methodName)
      .ToArray();

    Assert.NotEmpty(methods);

    Assert.All(methods, method =>
    {
      var returnType = method.ReturnType;
      var genericReturnType = returnType.IsGenericType
        ? returnType.GetGenericTypeDefinition()
        : null;
      var isAsync = returnType == typeof(Task)
        || returnType == typeof(ValueTask)
        || genericReturnType == typeof(Task<>)
        || genericReturnType == typeof(ValueTask<>);
      Assert.True(isAsync, $"{contract.Name}.{methodName} must remain asynchronous.");
      Assert.Contains(method.GetParameters(), parameter =>
        parameter.ParameterType == typeof(CancellationToken));
    });
  }

  private static void AssertReadOnlyProperty(Type contract, string propertyName, Type propertyType)
  {
    var property = contract.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

    Assert.NotNull(property);
    Assert.Equal(propertyType, property!.PropertyType);
    Assert.NotNull(property.GetMethod);
    Assert.Null(property.SetMethod);
  }

  private interface IContract { }

  private sealed class ContractImplementation : IContract { }
}
