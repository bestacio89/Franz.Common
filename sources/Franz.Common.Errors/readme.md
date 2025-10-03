# **Franz.Common.Errors**

A library for handling and standardizing errors and exceptions in .NET applications. This library simplifies error management, making it easier to implement structured and meaningful error responses.

---
- ** Current Version**: 1.6.2

---

## **Features**

- **Custom Exceptions**:
  - `ForbiddenException`
  - `FunctionalException`
  - `NotFoundException`
  - `PreconditionFailedException`
  - `TechnicalException`
  - `UnauthorizedException`
- **Base Exception**:
  - `ExceptionBase` for creating custom exceptions with consistent behavior.
- **Error Response**:
  - `ErrorResponse` for structuring API error responses.

---

## **Installation**

Install the package using NuGet Package Manager or the .NET CLI:

```bash
dotnet add package Franz.Common.Errors
```

---

## **Usage**

### **1. Handling Custom Exceptions**

Each exception is tailored for specific error scenarios. Here's an example of using `NotFoundException`:

```csharp
using Franz.Common.Errors;

public void FindResource(int id)
{
    var resource = GetResourceById(id);
    if (resource == null)
    {
        throw new NotFoundException($"Resource with ID {id} was not found.");
    }
}
```

### **2. Returning Structured Error Responses**

Use `ErrorResponse` to provide consistent error information in your APIs:

```csharp
using Franz.Common.Errors;

public IActionResult HandleException(Exception ex)
{
    var errorResponse = new ErrorResponse
    {
        StatusCode = 500,
        Message = ex.Message,
        Details = "Additional details about the error."
    };

    return StatusCode(errorResponse.StatusCode, errorResponse);
}
```

### **3. Creating Custom Exceptions**

Extend the `ExceptionBase` class to create your own exceptions with additional metadata:

```csharp
using Franz.Common.Errors;

public class CustomException : ExceptionBase
{
    public CustomException(string message) : base(message) { }
}
```

---

## **Exception Types Overview**

| Exception Type                | Description                                                   |
|-------------------------------|---------------------------------------------------------------|
| `ForbiddenException`          | Use for actions the user is not permitted to perform.         |
| `FunctionalException`         | Represents a domain or business logic error.                  | 
| `NotFoundException`           | Indicates that a requested resource could not be found.       |
| `PreconditionFailedException` | Indicates a failed precondition for the request.              |
| `TechnicalException`          | Represents an unexpected technical error.                     |
| `UnauthorizedException`       | Indicates a lack of authorization to access a resource.       |

---

## **Contributing**

We welcome contributions! Please follow these steps:
1. Clone the repository. @ https://github.com/bestacio89/Franz.Common/
2. Create a new branch for your feature or bug fix.
3. Submit a pull request for review.

---

## **License**

This library is licensed under the MIT License. See the LICENSE file for more details.

---

## **Changelog**

### Version 1.2.065
- Upgrade to .NET 9

### Version 1.3
- Upgraded to **.NET 9.0.8**
- Added **new features and improvements**
- Separated **business concepts** from **mediator concepts**
- Now compatible with both the **in-house mediator** and **MediatR**

### Version 1.4.1
- ?? Introduced **TestExceptions** for chaos engineering & demos  
  - `BananaRepublicException` (EN/FR)  
  - `MonsterException` (EN/FR)  
  - `VodkaCoffeePotException` (EN/FR)  
  - `FriendlyReminderException` (EN/FR)  
- ?? **Bilingual Support**: every test exception available in **English & French**  
- ?? **Consistent API**: wrapped under `TestExceptions` static class  
- ?? Purpose: make resilience demos, test failures, and dev workshops **fun & obvious**  
- ?? No impact on production code — these are **opt-in chaos exceptions**  

