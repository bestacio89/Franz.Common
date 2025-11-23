# Resilience Behavior – Franz.Common

This document describes the resilience primitives supported by Franz.

---

## 1. Retries

Default:

- exponential backoff  
- configurable per handler  
- logging at each retry  

---

## 2. Circuit Breakers

Applications may plug Polly or OTel-based circuit breakers into Franz.Http.

Franz preserves correlation IDs and error contracts through breakers.

---

## 3. Timeouts

Every pipeline behavior supports cancellation tokens.

Handlers *must* honor cancellation.

---

## 4. DLQ Behavior

- Final fallback for unrecoverable messages  
- Contains payload, error details, correlation  
- Can be reprocessed manually or automatically  

---

## 5. Graceful Degradation

Franz encourages:

- returning cached responses  
- fallback handlers  
- graceful shutdown with draining  

---

## 6. Summary

Franz provides mechanisms to build highly resilient, fault-tolerant service ecosystems with predictable behavior even under failure.
