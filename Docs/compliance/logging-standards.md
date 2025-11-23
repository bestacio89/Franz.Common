# Logging Standards – Franz.Common

This document describes standardized logging practices enforced by Franz.

---

## 1. Log Levels

- **TRACE** – internal pipeline details  
- **DEBUG** – developer-oriented diagnostics  
- **INFO** – successful operations  
- **WARN** – transient failures  
- **ERROR** – business errors or handler failures  
- **FATAL** – unrecoverable system-level errors  

---

## 2. Logging Fields

Every log entry should include:

- Timestamp  
- CorrelationId  
- MessageId (if messaging)  
- RequestPath (if HTTP)  
- Handler name  
- UserId (if authenticated)  
- Execution duration  

---

## 3. Sensitive Data

Do NOT log:

- passwords  
- tokens  
- personal documents  
- confidential metadata  

Mask PII where necessary.

---

## 4. Message Logging

For messaging:

- Log at receive  
- Log at success  
- Log at retry  
- Log at DLQ  

---

## 5. Error Logging

Errors must:

- include correlationId  
- include exception type  
- include contextual metadata  
- avoid excessive stack traces in production  

---

## 6. Summary

Franz ensures predictable, structured, compliant logs across all services.
