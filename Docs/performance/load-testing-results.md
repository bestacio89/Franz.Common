
# Load Testing – Franz.Common

This document captures recommended performance baselines and load testing methodology for services built on Franz.Common.

---

## 1. Methodology

Recommended tools:

- k6  
- Azure Load Testing  
- Locust  
- Vegeta  

Scenarios:

- sustained 500 RPS for 10 minutes  
- burst 2000 RPS  
- 95th percentile latency < 200ms  
- messaging throughput 1k–5k msgs/s  

---

## 2. HTTP Baseline

With Franz.Http:

- 50–80 μs overhead for middleware  
- 3–5 ms for mediator pipeline dispatch  
- negligible mapping/validation cost  

End-to-end typical latency:
- 20–60 ms under moderate load (excluding DB)

---

## 3. Messaging Baseline

Kafka example:

- processing: 5,000–12,000 msg/s  
- DLQ routing: 0.1% expected during spikes  

RabbitMQ:

- stable 2,000–6,000 msg/s  
- low-latency workers < 10ms  

---

## 4. Memory & CPU

Franz avoids:

- reflection-heavy patterns  
- heavy DI graphs  
- expression-tree generation at runtime  

Expected consumption:

- 80–160 MB per service under load  
- CPU usage linear with message throughput  

---

## 5. Summary

Franz introduces minimal overhead and supports high-throughput workloads in institutional environments