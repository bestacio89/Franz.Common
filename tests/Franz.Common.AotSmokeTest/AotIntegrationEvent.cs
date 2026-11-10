using Franz.Common.Mediator;

namespace Franz.Common.AotSmokeTest;

public sealed record AotIntegrationEvent(Guid EventId, string Payload) : IIntegrationEvent;
