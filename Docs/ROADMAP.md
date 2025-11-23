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
