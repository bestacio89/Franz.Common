# API Error Contract – Franz.Common

This defines the standardized error model returned by Franz.Http.

---

## 1. Standard Format

```json
{
  "type": "https://errors.domain.com/error-code",
  "title": "Human-readable error title",
  "detail": "Detailed explanation",
  "status": 400,
  "traceId": "00-abc123...",
  "errors": {
    "fieldName": ["error message 1", "error message 2"]
  }
}
```

## 2. Error Codes

Typical codes:

validation_error

not_found

unauthorized

forbidden

conflict

internal_error

unavailable

## 3. Validation Errors

Franz.Validation aggregates validation failures:
```json
"errors": {
    "email": ["Email is invalid"],
    "password": ["Password required"]
}
```

## 4. Benefits

- Determinism

- Easier debugging

- Client compatibility

- API governance

- Security & compliance

## 5. Summary

The Franz error contract provides a predictable model for all HTTP errors, ensuring uniform behavior across microservices.