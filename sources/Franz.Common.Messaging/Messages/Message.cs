#nullable enable

using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Headers;
using Franz.Common.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Franz.Common.Messaging.Messages;

public class Message : INotification, IEvent
{
  private Guid? _correlationId;
  private IDictionary<string, string[]> _headers = new MessageHeaders();

  public const string CorrelationHeader = "x-correlation-id";
  public const string CausationHeader = "x-causation-id";

  public Message()
  {
    Id = Guid.CreateVersion7();
    _correlationId = Id;
    SyncProjection();
  }

  public Message(string? body) : this()
  {
    Body = body;
  }

  public Message(string? body, IDictionary<string, string[]> headers) : this(body)
  {
    ArgumentNullException.ThrowIfNull(headers);

    var copiedHeaders = new MessageHeaders();
    foreach (var header in headers)
      copiedHeaders[header.Key] = header.Value;

    Headers = copiedHeaders;
  }

  public Guid Id { get; set; }
  public DateTimeOffset OccurredOn { get; set; } = DateTimeOffset.UtcNow;
  public virtual string? Body { get; set; }
  public MessageKind Kind { get; set; } = MessageKind.Command;

  public virtual IDictionary<string, string[]> Headers
  {
    get => _headers;
    set
    {
      _headers = value ?? throw new ArgumentNullException(nameof(value));
      HydrateFromHeaders(_headers);
    }
  }

  public virtual IDictionary<string, object> Properties { get; set; }
      = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

  [JsonIgnore]
  public virtual Guid? CorrelationId
  {
    get => _correlationId;
    set
    {
      _correlationId = value;

      if (value.HasValue)
        Properties[nameof(CorrelationId)] = value.Value;
      else
        Properties.Remove(nameof(CorrelationId));

      SyncProjection();
    }
  }

  public virtual Guid? AggregateId { get; set; }

  public virtual string? MessageType
  {
    get => Properties.TryGetValue(nameof(MessageType), out var value) ? value as string : null;
    set => SetProperty(nameof(MessageType), value);
  }

  [RequiresUnreferencedCode("Use the JsonTypeInfo overload for Native AOT.")]
  [RequiresDynamicCode("Use the JsonTypeInfo overload for Native AOT.")]
  public T? GetProperty<T>(string key)
  {
    if (!Properties.TryGetValue(key, out var value))
      return default;

    if (value is T typed)
      return typed;

    if (value is JsonElement json)
    {
      try
      {
        return json.Deserialize<T>(FranzJson.Default);
      }
      catch (Exception exception) when (
          exception is JsonException or NotSupportedException or InvalidOperationException)
      {
        return default;
      }
    }

    return default;
  }

  /// <summary>Reads a JSON-backed message property using application-provided source-generated metadata.</summary>
  public T? GetProperty<T>(string key, JsonTypeInfo<T> typeInfo)
  {
    ArgumentNullException.ThrowIfNull(typeInfo);

    if (!Properties.TryGetValue(key, out var value))
      return default;

    if (value is T typed)
      return typed;

    if (value is JsonElement json)
    {
      try
      {
        return json.Deserialize(typeInfo);
      }
      catch (Exception exception) when (
          exception is JsonException or NotSupportedException or InvalidOperationException)
      {
        return default;
      }
    }

    return default;
  }

  public void SetProperty<T>(string key, T value) => Properties[key] = value!;

  private void SyncProjection()
  {
    if (_correlationId.HasValue)
      _headers[CorrelationHeader] = new[] { _correlationId.Value.ToString("D") };
    else
      _headers.Remove(CorrelationHeader);
  }

  private void HydrateFromHeaders(IDictionary<string, string[]> headers)
  {
    if (headers.TryGetValue(CorrelationHeader, out var values) &&
        values.Length > 0 &&
        Guid.TryParse(values[0], out var correlationId))
    {
      _correlationId = correlationId;
      Properties[nameof(CorrelationId)] = correlationId;
    }
    else
    {
      SyncProjection();
    }
  }
}
