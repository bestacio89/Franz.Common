namespace Franz.Common.Errors;

public class ErrorResponse
{
    public string Message { get; set; } = null!;

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public string? StackTrace { get; set; }
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
}
