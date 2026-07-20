using Franz.Common.Mediator.Context;

namespace Franz.Common.Mediator;

public static class MediatorContextExtensions
{
  public static TRequest WithContext<TRequest>(this TRequest request) where TRequest : notnull
  {
    // Optional: attach context metadata to requests if needed
    return request;
  }
}