using Azure.Messaging.EventGrid;
using Franz.Common.Errors;
using Franz.Common.Messaging.AzureEventGrid.Ingress;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Franz.Common.Messaging.Hosting.Azure.EventGrid;

public static class EndpointRouteBuilderExtensions
{
  public static IEndpointConventionBuilder MapFranzAzureEventGrid(
      this IEndpointRouteBuilder endpoints,
      Action<AzureEventGridEndpointOptions>? configure = null)
  {
    var options = new AzureEventGridEndpointOptions();
    configure?.Invoke(options);

    return endpoints.MapPost(options.Route, async (
        HttpRequest request,
        IAzureEventGridIngress ingress,
        CancellationToken ct) =>
    {
      using var reader = new StreamReader(request.Body);
      var json = await reader.ReadToEndAsync(ct);

      if (string.IsNullOrWhiteSpace(json))
        throw new TechnicalException("Event Grid payload is empty.");

      // Parse batch (Event Grid posts arrays)
      var events = EventGridEvent.ParseMany(BinaryData.FromString(json));

      foreach (var evt in events)
        await ingress.IngestAsync(evt, ct);

      return Results.Ok();
    });
  }
}
