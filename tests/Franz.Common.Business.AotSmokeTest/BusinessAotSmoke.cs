using Franz.Common.Business.Domain;
using Franz.Common.Business.Domain.Factories;
using Franz.Common.Business.Extensions;
using Franz.Common.Mediator.Dispatchers;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Business.AotSmokeTest;

internal static class BusinessAotSmoke
{
  public static async Task<int> RunAsync()
  {
    var entity = new EntityFactory<Guid, AotEntity>(new AotGuidGenerator()).Create();
    if (entity.Id == Guid.Empty)
      return 1;

    var aggregate = new AggregateFactory<AotAggregate, AotEvent>(new AotGuidGenerator()).Create();
    if (aggregate.Id == Guid.Empty)
      return 2;

    var enumerations = Enumeration<int>.GetAll<AotEnumeration>();
    if (enumerations.Count != 2 ||
        Enumeration<int>.FromValue<AotEnumeration>(AotEnumeration.Ready.Id) != AotEnumeration.Ready)
      return 3;

    var services = new ServiceCollection();
    services.AddBusiness(typeof(AotCommandHandler).Assembly);

    using var provider = services.BuildServiceProvider();
    using var scope = provider.CreateScope();
    var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();
    var result = await dispatcher.SendAsync<string>(new AotCommand("business-aot"));

    return result == "business-aot" ? 0 : 4;
  }
}