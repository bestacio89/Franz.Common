# Franz.Common.Errors

> Exception types and a small error payload contract for expressing application failures.

## Installation

```bash
dotnet add package Franz.Common.Errors
```

## Exception hierarchy

The package contains two related groups:

- `ExceptionBase` is an abstract serializable base class with parameterless, message, and message-plus-inner-exception constructors.
- `NotFoundException`, `PreconditionFailedException`, `TechnicalException`, `UnauthorizedException`, and `FunctionalException` derive from `ExceptionBase`.
- `ForbiddenException` derives directly from `Exception`.
- `BusinessException` also derives directly from `Exception` and carries a stable `Code` plus read-only `Metadata`.

The exception types provide semantic classification. They do not themselves map to HTTP status codes, write logs, or serialize an API response. The application boundary must define that mapping.

## Usage

```csharp
using Franz.Common.Errors;

public Order GetOrder(Guid id)
{
    var order = repository.Find(id);

    if (order is null)
        throw new NotFoundException($"Order '{id}' was not found.");

    return order;
}
```

For machine-readable business failures, use `BusinessException` and stable codes that clients can depend on:

```csharp
throw new BusinessException(
    "order.state.invalid",
    "The order cannot be confirmed in its current state.",
    new Dictionary<string, object?>
    {
        ["orderId"] = orderId,
        ["state"] = currentState
    });
```

Metadata is copied into an internal dictionary at construction. Treat it as diagnostic data: do not include credentials, tokens, personal secrets, or other sensitive values.

## ErrorResponse

`ErrorResponse` is a minimal mutable payload with a required-by-convention `Message` string and optional `StackTrace`. It does not contain a status code, error code, details collection, or automatic exception conversion.

```csharp
var response = new ErrorResponse
{
    Message = "The requested resource was not found."
};
```

Do not expose stack traces to untrusted clients in production. Prefer a centrally defined exception-to-response policy at the API boundary.

## Test failure helpers

`TestExceptions` provides named factories for intentionally throwing ordinary `Exception` instances in demonstrations and tests: `BananaRepublic()`, `Monster()`, `VodkaCoffeePot()`, and `FriendlyReminder()`, each with a French variant. These are not production exception types and carry no structured error contract.

## Design guidance

Use exceptions for exceptional control flow and boundary translation, not as a replacement for expected validation results. Keep business error codes stable, separate from transport-specific HTTP semantics.

## License

MIT. See [LICENSE](../../LICENSE).
