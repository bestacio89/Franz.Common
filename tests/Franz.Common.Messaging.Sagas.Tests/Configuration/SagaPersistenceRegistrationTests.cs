using FluentAssertions;
using Franz.Common.Messaging.Sagas.Configuration;
using Franz.Common.Messaging.Sagas.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Messaging.Sagas.Tests.Configuration;

public sealed class SagaPersistenceRegistrationTests
{
  [Theory]
  [InlineData("Memory")]
  [InlineData("EntityFramework")]
  [InlineData("Mongo")]
  [InlineData("Cosmos")]
  public void AddFranzSagas_RegistersExactlyOneScopedRepositoryForSelectedProvider(string persistence)
  {
    var services = new ServiceCollection();

    services.AddFranzSagas(options => options.Persistence = persistence);

    var repositoryDescriptors = services
        .Where(descriptor => descriptor.ServiceType == typeof(ISagaRepository))
        .ToArray();

    repositoryDescriptors.Should().ContainSingle();
    repositoryDescriptors[0].Lifetime.Should().Be(ServiceLifetime.Scoped);
    repositoryDescriptors[0].ImplementationFactory.Should().NotBeNull();
  }

  [Theory]
  [InlineData("Kafka")]
  [InlineData("Redis")]
  [InlineData("Unknown")]
  public void AddFranzSagas_RejectsPersistenceProvidersWithoutAnImplementedContract(string persistence)
  {
    var services = new ServiceCollection();

    var act = () => services.AddFranzSagas(options => options.Persistence = persistence);

    act.Should().Throw<NotSupportedException>()
        .WithMessage($"*{persistence}*");
  }
}
