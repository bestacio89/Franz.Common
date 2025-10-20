# **Franz.Common.Testing**

A library within the **Franz Framework** designed to enhance unit testing in .NET applications. This package simplifies test development by integrating with popular testing frameworks and providing utilities for mock setups and fluent assertions.

---

## **Features**

- **Unit Testing Simplification**:
  - Includes `UnitTest` base class to standardize test setup and execution.
- **Fluent Assertions**:
  - Integrates **FluentAssertions** for writing readable and expressive assertions.
- **Mocking Support**:
  - Utilizes **Moq** and **MockQueryable.Moq** for creating and managing mock objects.
  - Includes **Moq.AutoMock** for automatic mock generation.
- **Resource Management**:
  - Provides `Resources.resx` for managing test-specific resources.

---

## **Version Information**

-- **Current Version**: 1.6.15
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package leverages the following dependencies:
- **FluentAssertions** (6.12.0): For fluent and expressive test assertions.
- **MockQueryable.Moq** (7.0.0): Simplifies mocking IQueryable and LINQ queries.
- **Moq.AutoMock** (3.5.0): Automates the creation of mocks for dependency injection scenarios.

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
dotnet add package Franz.Common.Testing  
```

---

## **Usage**

### **1. Create Unit Tests Using the Base Class**

Leverage the `UnitTest` base class for consistent test initialization:

```csharp
using Franz.Common.Testing;

public class MyUnitTest : UnitTest
{
    [Fact]
    public void TestExample()
    {
        // Arrange
        var value = 1;

        // Act
        value++;

        // Assert
        value.Should().Be(2);
    }
}
```

### **2. Use Fluent Assertions**

Write expressive and readable assertions with **FluentAssertions**:

```csharp
[Fact]
public void String_ShouldContainExpectedValue()
{
    // Arrange
    var result = "Hello, Franz Framework!";

    // Assert
    result.Should().Contain("Franz");
}
```

### **3. Mock LINQ Queries**

Use **MockQueryable.Moq** to mock IQueryable data sources:

```csharp
using MockQueryable.Moq;
using Moq;

[Fact]
public void ShouldReturnFilteredData()
{
    // Arrange
    var mockData = new List<string> { "Alice", "Bob", "Charlie" }.AsQueryable().BuildMock();
    var repository = new Mock<IRepository>();
    repository.Setup(r => r.GetData()).Returns(mockData.Object);

    // Act
    var result = repository.Object.GetData().Where(x => x.Contains("a"));

    // Assert
    result.Should().Contain("Charlie").And.NotContain("Bob");
}
```

### **4. Automate Mock Creation**

Simplify mock setups with **Moq.AutoMock**:

```csharp
using Moq.AutoMock;

[Fact]
public void ShouldInjectDependenciesAutomatically()
{
    // Arrange
    var mocker = new AutoMocker();
    var service = mocker.CreateInstance<MyService>();

    // Act
    service.DoSomething();

    // Assert
    mocker.GetMock<IDependency>().Verify(d => d.PerformAction(), Times.Once);
}
```

---

## **Integration with Franz Framework**

The **Franz.Common.Testing** package integrates seamlessly with other **Franz Framework** packages, providing consistent and efficient testing utilities for the ecosystem.

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
- Added `UnitTest` base class for standardized test setup.
- Integrated **FluentAssertions** for fluent and expressive assertions.
- Added support for **MockQueryable.Moq** and **Moq.AutoMock** for efficient mocking.
- Provided `Resources.resx` for managing test-specific resources.

---


### Version 1.3
- Upgraded to **.NET 9.0.8**
- Added **new features and improvements**
- Separated **business concepts** from **mediator concepts**
- Now compatible with both the **in-house mediator** and **MediatR**
