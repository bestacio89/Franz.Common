# System Architecture – Franz.Common

This document provides an architectural overview of the Franz.Common ecosystem following a C4-inspired representation. It explains how Franz modules interact with each other and how consuming services are expected to integrate with the framework.

---

## 1. Purpose

Franz.Common is a modular, correctness-first architecture toolkit for building deterministic, maintainable, and scalable .NET services.

It provides:

- HTTP pipeline conventions,
- CQRS / Mediator patterns with pluggable behaviors,
- Validation pipeline,
- Messaging orchestration (Kafka/RabbitMQ),
- Logging, correlation, diagnostics,
- Persistence abstractions,
- Multi-tenancy helpers.

Franz is intended to standardize patterns across multiple teams and services, especially in large project portfolios (EU institutions, enterprise digital transformations, multi-vendor environments).

---

## 2. Context Diagram (C4 Level 1)

**Actors:**
- End-users
- External systems (REST APIs, message brokers)
- Databases
- Monitoring & Logging systems

**System:**
A microservice built on Franz.Common.

**High-level interactions:**
1. User/API Client → Service via HTTP  
2. External systems → Service via Messaging (Kafka/RabbitMQ)  
3. Service → Databases  
4. Service → Monitoring/Tracing platforms (OpenTelemetry, ELK, Grafana, Azure Monitor, etc.)

---

## 3. Container View (C4 Level 2)

A service using Franz is composed of:

### ✔ API Container
- ASP.NET Core WebHost  
- Franz.Http middleware  
- Controllers / Minimal APIs  
- Authentication & Authorization

### ✔ Application Container
- Franz.Mediator request pipeline  
- Commands, Queries, Handlers  
- Validation Behavior  
- Logging Behavior  
- Transactions Behavior (optional)

### ✔ Domain Container
- Entities, Aggregates, Value Objects  
- Domain Events  
- Domain Services  

### ✔ Infrastructure Container
- Franz Messaging modules (Kafka/RabbitMQ)  
- Persistence (SQL, MongoDB, CosmosDB, etc.)  
- External API clients  
- Caching  
- Logging sinks  

---

## 4. Component View (C4 Level 3)

### 🔹 Franz.Common
Shared abstractions:
- Correlation ID
- Serialization helpers
- Result types
- Common exceptions

### 🔹 Franz.Common.Http
- HTTP conventions  
- Error translation  
- Correlation ID middleware  
- API response normalization  

### 🔹 Franz.Common.Mediator
- Commands / Queries  
- Request pipeline behaviors  
- Decorators for validation, logging, tracing  

### 🔹 Franz.Common.Messaging.\*
- Producers  
- Consumers  
- Outbox pattern  
- Dead Letter queues  
- Retry policies  
- Message envelopes with correlation  

### 🔹 Franz.Common.Validation
- Validation middleware  
- FluentValidation integration  
- Consistent error contracts  

### 🔹 Franz.Common.Logging
- Structured logging with Serilog  
- Correlation ID enrichment  
- Request/processing logs  

---

## 5. Cross-Cutting Concerns

Franz enforces:

### ✔ Deterministic error contracts  
Standardized formats returned for all HTTP errors.

### ✔ Observability  
Correlation IDs carried across all layers.

### ✔ Resilience  
Retry strategies, DLQ routing, idempotency for messaging.

### ✔ Consistency  
Unified patterns across services and teams.

---

## 6. Deployment View (C4 Level 4)

Services built with Franz.Common typically run in:

- Docker / Kubernetes clusters  
- Azure App Service / Azure Container Apps  
- VM-based deployments in secure institutions  
- On-premise clusters  

Messaging + storage are external and pluggable.

---

## 7. Summary

Franz.Common is not a monolith—it's a distributed architecture foundation that enables teams to build scalable, testable, and maintainable services that behave consistently across a large enterprise environment.
