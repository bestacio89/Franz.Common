namespace Franz.Common.Mediator.Errors
{
  public sealed class Error
  {
    public string Code { get; }
    public string Message { get; }
    public IDictionary<string, object?> Metadata { get; }

    public Error(string code, string message, IDictionary<string, object?>? metadata = null)
    {
      Code = code;
      Message = message;
      Metadata = metadata ?? new Dictionary<string, object?>();
    }

    public override string ToString() => $"{Code}: {Message}";
  }
}
