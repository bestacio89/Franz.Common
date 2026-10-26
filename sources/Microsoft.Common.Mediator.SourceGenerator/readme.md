# Franz.Common.Mediator.SourceGenerator

Roslyn source generator used by Franz.Common.Mediator to generate deterministic mediator handler registrations at compile time.

## Compatibility

- Targets .NET Standard 2.0 for analyzer and source-generator compatibility.
- Uses Microsoft.CodeAnalysis 5.6.
- Package version follows the Franz.Common release version defined in `common.targets`.

## Usage

The generator is consumed by the Franz.Common.Mediator package and is normally brought in through that package's project/package dependency graph. Applications should use the documented Mediator registration APIs rather than referencing the generator directly unless they have a specific analyzer integration requirement.
