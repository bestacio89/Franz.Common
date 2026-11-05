# Franz.Common.Mediator.SourceGenerator

> Roslyn source generator that discovers Mediator handlers and emits deterministic compile-time registration code.

## Overview

Roslyn source generator that discovers Mediator handlers and emits deterministic compile-time registration code.

## Installation

The source generator is included with the Mediator package; applications should install the supported entry point:

```bash
dotnet add package Franz.Common.Mediator
```

## Compatibility

- Targets .NET Standard 2.0 for analyzer and source-generator compatibility.
- Uses Microsoft.CodeAnalysis 5.6.
- Package version follows the Franz.Common release version defined in `common.targets`.

## Usage

The generator is consumed by the Franz.Common.Mediator package and is normally brought in through that package's project/package dependency graph. Applications should use the documented Mediator registration APIs rather than referencing the generator directly unless they have a specific analyzer integration requirement.

## Diagnostics

The generator emits warning `FRZMED001` when a statically typed call to
`IDispatcher.SendAsync`, `PublishNotificationAsync`, or `PublishEventAsync` uses a concrete request type for which
the current compilation contains no matching generated handler registration.

The diagnostic intentionally does not report calls whose request is typed only
as an interface, abstract type, or generic type parameter: the concrete request
cannot be established reliably at compile time. Those paths retain the runtime
generated-registry guard, which throws a descriptive exception when no invoker
exists. The warning is not a substitute for runtime integration tests.


## License

Licensed under the MIT License. See the repository [LICENSE](../../LICENSE) for details.
