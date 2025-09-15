using Franz.Common.Mediator.Validation;

namespace Franz.Common.Mediator.Pipelines.Validation
{
  public sealed class ValidationResult
  {
    public bool IsValid => Errors.Count == 0;
    public List<ValidationError> Errors { get; }

    public ValidationResult(IEnumerable<ValidationError>? errors = null)
    {
      Errors = errors?.ToList() ?? new List<ValidationError>();
    }

    public static ValidationResult Success() => new();
    public static ValidationResult Failure(IEnumerable<ValidationError> errors) => new(errors);
  }
}