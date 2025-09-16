using Franz.Common.Mediator.Errors;
using System;

namespace Franz.Common.Mediator.Results
{
  public class Result
  {
    public bool IsSuccess { get; }
    public Error? Error { get; }

    protected Result(bool isSuccess, Error? error)
    {
      IsSuccess = isSuccess;
      Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(Error error) => new(false, error);
  }

  public class Result<T> : Result
  {
    public T? Value { get; }

    private Result(bool isSuccess, T? value, Error? error)
        : base(isSuccess, error)
    {
      Value = value;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static new Result<T> Failure(Error error) => new(false, default, error);
  }
}

