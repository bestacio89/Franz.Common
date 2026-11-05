# Franz.Common.IO

> Asynchronous file, stream, and filesystem utilities with consistent resource handling for .NET applications.

## Overview

Asynchronous file, stream, and filesystem utilities with consistent resource handling for .NET applications.

## Installation

```bash
dotnet add package Franz.Common.IO
```

## Design principles

- **Explicit composition:** register only the capabilities the application needs.
- **Clear boundaries:** keep domain and application contracts independent from provider-specific infrastructure where the package's role permits it.
- **Predictable behavior:** make configuration, failure modes, and lifecycle ownership visible rather than relying on hidden conventions.
- **Operational readiness:** validate security, resilience, diagnostics, and compatibility for the actual deployment environment.

## Usage and configuration

Follow the package-specific setup, configuration, and code examples below. Only configure external services when this package requires them, and keep secrets outside source control.

## Package-specific guide

A utility library within the **Franz Framework** designed to simplify input/output operations in **.NET** applications. This package provides streamlined solutions for managing temporary files and file streams, ensuring efficient and clean file handling.

---

## **Features**

- **Temporary File Management**:
  - `DeleteTemporaryFileAfterReadingStream` for safely handling temporary files and ensuring cleanup after file operations.

---

## **Version Information**

**Current Version:** v2.3.4
- Part of the open-source Franz.Common ecosystem.

---

## **Dependencies**

This package is designed to operate independently and does not have external dependencies, making it lightweight and easy to integrate.

---

## **Installation**

### Install from NuGet.org

```bash
dotnet add package Franz.Common.IO
```

```

Install the package:

```bash
dotnet add package Franz.Common.IO
```

---

## **Usage**

### **1. Handle Temporary Files**

Use `DeleteTemporaryFileAfterReadingStream` to ensure temporary files are deleted after being read:

```csharp
using Franz.Common.IO;

public class MyService
{
    public void ProcessFile(Stream stream)
    {
        using var temporaryFileStream = new DeleteTemporaryFileAfterReadingStream(stream);

        // Read or process the stream
        // The temporary file will be deleted after the stream is closed
    }
}
```

This approach ensures temporary files are cleaned up automatically after their intended usage.

---

## **Integration with Franz Framework**

The **Franz.Common.IO** package is a lightweight addition to the **Franz Framework** and can be used alongside other Franz libraries without any dependencies.

---

## **Contributing**

Contributions are welcome under the MIT License. To propose a change:
1. Clone the repository. @ https://github.com/bestacio89/Franz.Common/
2. Create a feature branch.
3. Submit a pull request for review.

---

## License

Licensed under the MIT License. See the repository [LICENSE](../../LICENSE) for details.

## **Changelog**

### Version 1.2.65
- Upgrade version to .net 9


### Version 1.3
- Upgraded to **.NET 9.0.8**
- Added **new features and improvements**
- Separated **business concepts** from **mediator concepts**
- Now compatible with both the **in-house mediator** and **MediatR**

### Version 1.6.20
- Updated to **.NET 10.0**

### v2.0.1 – Internal Modernization

- Messaging and infrastructure refactored for async, thread-safety, and modern .NET 10 patterns.
- All APIs remain fully backward compatible.
- Tests, listeners, and pipeline components modernized.
