namespace Franz.Common.Mediator.Validation
{
  public sealed class ValidationError
  {
    public string PropertyName { get; }
    public string ErrorMessage { get; }

    public ValidationError(string propertyName, string errorMessage)
    {
      PropertyName = propertyName;
      ErrorMessage = errorMessage;
    }

    public override string ToString() =>
        string.IsNullOrWhiteSpace(PropertyName)
            ? ErrorMessage
            : $"{PropertyName}: {ErrorMessage}";
  }
}
