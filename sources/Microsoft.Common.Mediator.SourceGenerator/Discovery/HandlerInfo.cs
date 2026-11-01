namespace Franz.Common.Mediator.SourceGenerator.Discovery;

internal sealed record HandlerInfo(
    string ServiceType,
    string ImplementationType,
    string HandlerKind,
    string RequestType,
    string? ResponseType);
