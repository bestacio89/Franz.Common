using Franz.Common.Mediator.Errors;

namespace Franz.Common.Mediator.Results
{
  public static class ResultExtensions
  {
    public static Result<T> ToResult<T>(this T value) =>
        Result<T>.Success(value);

    public static Result<T> ToFailure<T>(this string message) =>
        Result<T>.Failure(new Error("Error", message, null));

    public static Result<T> ToFailure<T>(this Error error) =>
        Result<T>.Failure(error);
  }
}