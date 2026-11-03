# Entity auditing dispatch benchmarks

This BenchmarkDotNet project compares the previous audit-dispatch implementation with the current direct contract dispatch.

| Scenario | Implementation |
| --- | --- |
| Baseline | Per-entity `IEntity<TKey>` interface discovery followed by dynamic lifecycle invocation |
| Candidate | Generic change-tracker equivalent: direct `IAuditableEntity` interface dispatch |

The benchmark runs at 1, 100, and 1,000 tracked entities and records elapsed time and allocated bytes using BenchmarkDotNet's `MemoryDiagnoser`.

## Run locally

From the repository root:

```bash
dotnet run --project benchmarks/Franz.Common.EntityFramework.Auditing.Benchmarks/Franz.Common.EntityFramework.Auditing.Benchmarks.csproj --configuration Release
```

The dedicated GitHub Actions workflow runs the same benchmark on the pull request and publishes the Markdown/JSON reports as an artifact.

## Scope and interpretation

This benchmark isolates entity audit dispatch: interface discovery, dynamic binding, and lifecycle method invocation. It deliberately excludes EF Core change-tracker enumeration, database provider I/O, and transaction costs. Results quantify the dispatch-path difference, not end-to-end `SaveChanges` latency.

Both implementations execute the same lifecycle operation against the same entity types and batch sizes on the same runner. Compare mean time and allocated bytes for each matching batch size. Do not generalize these results to full persistence throughput.
