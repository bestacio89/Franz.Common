namespace Franz.Common.Mediator.Outcome
{
  /// <summary>
  /// Represents a transport-level outcome (status + optional body).
  /// Framework-agnostic: can be mapped to HTTP, gRPC, etc.
  /// </summary>
  public readonly struct Outcome
  {
    public int StatusCode { get; }
    public object? Body { get; }

    private Outcome(int statusCode, object? body = null)
    {
      StatusCode = statusCode;
      Body = body;
    }

    // ✅ Success helpers
    public static Outcome Ok() => new Outcome(200);
    public static Outcome Ok<T>(T body) => new Outcome(200, body);

    // ❌ Failure helper
    public static Outcome Fail(int statusCode, object error) =>
        new Outcome(statusCode, error);

    public override string ToString() =>
        $"Outcome(StatusCode: {StatusCode}, Body: {Body})";
  }
}
