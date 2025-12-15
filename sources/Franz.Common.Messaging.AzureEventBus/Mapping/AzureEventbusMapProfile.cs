using Franz.Common.Mapping.Core;
using Franz.Common.Messaging.AzureEventBus.Contracts;

namespace Franz.Common.Messaging.AzureEventBus.Mapping;

/// <summary>
/// Registers mappings between domain events and Azure transport payloads.
/// </summary>
internal sealed class AzureEventBusMapProfile : FranzMapProfile
{
  public AzureEventBusMapProfile()
  {
    // Generic mapping: any event -> payload
    CreateMap<object, AzureEventBusPayload>()
        .ConstructUsing(src =>
            new AzureEventBusPayload(
                EventType: src.GetType().FullName!,
                Data: src
            ));
  }
}
