# Franz.Common.Caching.Excel

Optional Excel export integration for cache observability.

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
