# Franz.Common – Roadmap

This document outlines planned and potential future enhancements.

---

## Near-term (0–6 months)

- gRPC transport integration (Franz.Common.Grpc):
  - standardized server & client configuration,
  - shared interceptors (logging, tracing, auth).
- GraphQL support (optional):
  - conventions for schema composition,
  - integration with existing mediator pipelines.
- Messaging improvements:
  - enhanced outbox/inbox support,
  - first-class saga/long-running process helper APIs.

## Mediator runtime modernization

Mediator improvements follow a correctness-first, measurement-driven sequence while preserving the current .NET 10 and Franz.Common 2.3.3 baseline. The detailed phases, compatibility constraints, and exit criteria are maintained in [the Mediator upgrade roadmap](mediator-upgrade-roadmap.md).

---

## Mid-term (6–18 months)

- Multi-cloud deployment helpers:
  - templates for Azure, AWS, GCP,
  - integration guidance for messaging & storage options.
- Observability package:
  - OpenTelemetry-based tracing integration,
  - ready-to-use metrics and logging enrichers.
- Additional persistence adapters:
  - opinionated patterns for Postgres,
  - extended support for Cosmos DB / Mongo.

---

## Long-term

- Architecture rules enforcement:
  - guidance and tooling to integrate with ArchUnitNET / Roslyn analyzers.
- Richer developer tooling:
  - project templates,
  - CLIs for scaffolding services using Franz conventions.

All items are subject to reprioritization based on real-world needs.

---

## Framework contract hardening sequence

The active correctness-first framework roadmap is delivered sequentially:

1. **Public contracts and API shape** — complete.
2. **Dependency injection and lifecycle guarantees** — complete.
3. **Mediator and mapper generated dispatch, trimming, and Native AOT validation** — complete in PR #180.
4. **HTTP contracts, authentication, validation, and tenant behavior** — active in PR #181.
5. **Persistence, transactions, concurrency, bulk operations, and provider parity** — next.
6. **Observability, resilience, retries, timeouts, and recovery**.
7. **Messaging delivery guarantees, ordering, and crash recovery**.
8. **Release hardening, security, compatibility, and reproducible CI**.

Each phase must retain focused behavioral and architecture tests, update the framework contract coverage ledger, and pass the full CI gates before the next phase begins.
