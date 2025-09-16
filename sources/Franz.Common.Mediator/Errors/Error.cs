namespace Franz.Common.Mediator.Errors
{
  public sealed class Error
  {
    // ✅ Common error codes centralized here
    public static class Codes
    {
      public const string NotFound = "NotFound";
      public const string Validation = "Validation";
      public const string Conflict = "Conflict";
      public const string Unexpected = "Unexpected";
    }

    public string Code { get; }
    public string Message { get; }
    public IDictionary<string, object?> Metadata { get; }

    public Error(string code, string message, IDictionary<string, object?>? metadata = null)
    {
      Code = code;
      Message = message;
      Metadata = metadata ?? new Dictionary<string, object?>();
    }

    // ✅ Factory helpers
    public static Error NotFound(string entity, object id) =>
        new(Codes.NotFound, $"{entity} with ID '{id}' was not found.");

    public static Error Unexpected(string message) =>
        new(Codes.Unexpected, message);

    public static Error Conflict(string entity, object id) =>
        new(Codes.Conflict, $"{entity} with ID '{id}' is in conflict.");

    public static Error Validation(string property, string message) =>
        new(Codes.Validation, $"{property}: {message}");

    public override string ToString() => $"{Code}: {Message}";
  }
}
