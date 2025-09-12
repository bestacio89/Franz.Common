namespace Franz.Common.MultiTenancy;

public sealed class DomainResolutionResult
{
  public bool Success { get; }
  public Guid? DomainId { get; }
  public string Message { get; }

  private DomainResolutionResult(bool success, Guid? domainId, string message)
  {
    Success = success;
    DomainId = domainId;
    Message = message;
  }

  public static DomainResolutionResult Succeeded(Guid domainId, string message = "")
      => new DomainResolutionResult(true, domainId, message);

  public static DomainResolutionResult FailedResult(string message = "")
      => new DomainResolutionResult(false, null, message);

  public override string ToString()
  {
    return Success
        ? $"DomainResolutionResult: Success, DomainId={DomainId}, Message={Message}"
        : $"DomainResolutionResult: Failed, Message={Message}";
  }
}
