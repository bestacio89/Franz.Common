# Runtime Lifecycle – Franz.Common

This document describes the bootstrap sequence of a service built on Franz.Common.

---

## 1. Initial Host Setup

1. Configuration loading  
2. Logging framework initialization  
3. DI container build  
4. Registration of Franz modules (Http, Mediator, Validation, etc.)

---

## 2. Franz Boot Sequence

### 2.1 Pipeline Initialization  
- Correlation middleware  
- Error handling middleware  
- Response normalization  
- Telemetry instrumentation  
- Authentication/Authorization  
- Routing

### 2.2 Application Initialization  
- Load command/query handlers  
- Register validation rules  
- Activate mediator behaviors  
- Resolve mapper profiles  
- Initialize job schedulers (optional)

### 2.3 Infrastructure Activation  
- Database connection warming  
- Messaging consumers start (Kafka/RabbitMQ)  
- Outbox dispatcher activation  
- Cache warmups  
- External clients creation  

---

## 3. Request Lifecycle (HTTP)

1. Request enters HTTP middleware  
2. Correlation ID extracted/generated  
3. Authentication & authorization  
4. Validation pipeline  
5. Mediator dispatch  
6. Controller returns standardized result  
7. Logs emitted  
8. Traces pushed to OpenTelemetry

---

## 4. Message Lifecycle (Kafka/RabbitMQ)

1. Raw message consumed  
2. Envelope parsed  
3. Handler resolved & invoked  
4. Retries applied if needed  
5. Result acknowledged or moved to DLQ  
6. Logs + metrics emitted  

---

## 5. Shutdown Lifecycle

- Stop message consumers  
- Flush outbox  
- Complete in-flight HTTP requests  
- Flush logs & traces  
- Dispose DI container  
- Graceful shutdown (SIGTERM)

---

## 6. Summary

Franz defines a deterministic boot and runtime pipeline that ensures every service behaves consistently across environments and teams.
