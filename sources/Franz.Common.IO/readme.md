# **Franz.Common.IO**

A utility library within the **Franz Framework** designed to simplify input/output operations in **.NET** applications. This package provides streamlined solutions for managing temporary files and file streams, ensuring efficient and clean file handling.

---

## **Features**

- **Temporary File Management**:
  - `DeleteTemporaryFileAfterReadingStream` for safely handling temporary files and ensuring cleanup after file operations.

---

## **Version Information**

- **Current Version**:  1.3.5
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package is designed to operate independently and does not have external dependencies, making it lightweight and easy to integrate.

---

## **Installation**

### **From Private Azure Feed**
Since this package is hosted privately, configure your NuGet client:

```bash
dotnet nuget add source "https://your-private-feed-url" \
  --name "AzurePrivateFeed" \
  --username "YourAzureUsername" \
  --password "YourAzurePassword" \
  --store-password-in-clear-text
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

This package is part of a private framework. Contributions are limited to the internal development team. If you have access, follow these steps:
1. Clone the repository. @ https://github.com/bestacio89/Franz.Common/
2. Create a feature branch.
3. Submit a pull request for review.

---

## **License**

This library is licensed under the MIT License. See the `LICENSE` file for more details.

---

## **Changelog**

### Version 1.2.65
- Upgrade version to .net 9


### Version 1.3
- Upgraded to **.NET 9.0.8**
- Added **new features and improvements**
- Separated **business concepts** from **mediator concepts**
- Now compatible with both the **in-house mediator** and **MediatR**