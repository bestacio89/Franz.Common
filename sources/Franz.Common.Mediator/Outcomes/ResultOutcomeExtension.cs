using Franz.Common.Mediator.Errors;
using Franz.Common.Mediator.Results;

namespace Franz.Common.Mediator.Outcome
{
  /// <summary>
  /// Extensions to translate Result/Result<T> into transport-level Outcome.
  /// </summary>
  public static class ResultOutcomeExtensions
  {
    public static Outcome ToOutcome(this Result result)
    {
      if (result.IsSuccess)
        return Outcome.Ok();

      return MapError(result.Error!);
    }

    public static Outcome ToOutcome<T>(this Result<T> result)
    {
      if (result.IsSuccess)
        return Outcome.Ok(result.Value);

      return MapError(result.Error!);
    }

    private static Outcome MapError(Error error)
    {
      return error.Code switch
      {
        Error.Codes.NotFound => Outcome.Fail(404, error),
        Error.Codes.Validation => Outcome.Fail(400, error),
        Error.Codes.Conflict => Outcome.Fail(409, error),
        Error.Codes.Unexpected => Outcome.Fail(500, error),
        _ => Outcome.Fail(400, error) // fallback
      };
    }
  }
}
