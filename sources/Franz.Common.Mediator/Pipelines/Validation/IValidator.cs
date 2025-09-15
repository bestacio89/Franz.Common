namespace Franz.Common.Mediator.Pipelines.Validation
{
  public interface IValidator<TRequest>
  {
    Task<ValidationResult> ValidateAsync(TRequest instance, CancellationToken cancellationToken);
  }
}