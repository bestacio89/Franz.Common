# Franz.Common.Caching.Excel

> Optional EPPlus-based Excel export for Franz.Common cache observer statistics.

## Overview

Franz.Common.Caching.Excel provides an opt-in Excel observer without adding the
EPPlus dependency to applications that use only the core caching package.

This package contains `ExcelCacheObserver`. It is intentionally separate from
`Franz.Common.Caching` so applications that do not export cache statistics do
not take an EPPlus dependency.

## Installation

```bash
dotnet add package Franz.Common.Caching
dotnet add package Franz.Common.Caching.Excel
```

## Usage

Register the observer alongside the core cache provider:

```csharp
services.AddFranzMemoryCaching();
services.AddCacheObserver<ExcelCacheObserver>();
services.AddObservableCaching();
```

Export from the observer instance using `ExportToExcel(path)`.

EPPlus 8 requires the consuming application to configure its license according
to its own licensing terms before creating an `ExcelPackage`. This package does
not select a license or embed a license key on the application's behalf.


## License

Licensed under the MIT License. See the repository [LICENSE](../../LICENSE).
