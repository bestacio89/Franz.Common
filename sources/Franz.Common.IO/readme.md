# Franz.Common.IO

> A temporary-file-backed stream that deletes its backing file when disposed.

## Overview

A temporary-file-backed stream that deletes its backing file when disposed.

## Installation

```bash
dotnet add package Franz.Common.IO
```

## DeleteTemporaryFileAfterReadingStream

The package currently exposes `DeleteTemporaryFileAfterReadingStream`, a `FileStream` subclass. Its parameterless constructor creates a temporary file using `Path.GetTempFileName()` and opens it for writing with `FileMode.Create`.

```csharp
using Franz.Common.IO;

await using (var temporary = new DeleteTemporaryFileAfterReadingStream())
{
    await source.CopyToAsync(temporary);
    temporary.Position = 0;

    // Consume the temporary stream before leaving this scope.
    await temporary.CopyToAsync(destination);
}
// The stream is closed and its backing temporary file is deleted.
```

## Ownership and lifecycle

The stream owns the temporary file it creates. Disposing the stream first closes the underlying `FileStream`, then deletes the file if it still exists. Dispose it deterministically with `using` or `await using`; do not treat the backing path as durable storage.

The type does not wrap an arbitrary caller-provided stream, and it does not delete a source file after reading. It creates and owns its own temporary file.

## Operational considerations

- Temporary storage capacity and permissions are environment concerns; ensure the process can create files in the platform temporary directory.
- Temporary files may contain sensitive data. Apply appropriate host-level filesystem protections and avoid logging their contents.
- The implementation deletes the file during synchronous disposal. Handle cleanup failures according to the application's operational policy.
- This is a file-backed stream, not an in-memory buffer; account for disk I/O and temporary storage usage.

## License

MIT. See [LICENSE](../../LICENSE).
