# Integration Architecture – Franz.Common

This document explains how Franz.Common fits inside a larger cloud or on-premise environment.

---

## 1. High-Level Integration

Franz integrates with:

- HTTP APIs  
- Message brokers  
- Databases  
- Logging stacks  
- Cloud provider services  

Through:

- adapters  
- standardized interfaces  
- minimal assumptions  

---

## 2. Franz in a Microservice Environment

+-----------------------+
| API Gateway / Firewall|
+-----------------------+
|
v
+-----------------------+
| Service (Franz-based) |
| - Franz.Http |
| - Franz.Mediator |
| - Franz.Validation |
| - Domain Logic |
| - Franz.Messaging |
+-----------------------+
| | |
v v v
Database Broker Monitoring


---

## 3. Logging & Monitoring

Franz produces:

- structured logs (Serilog)  
- OpenTelemetry traces  
- metrics (Prometheus/Azure Monitor)  

Compatible with:

- Kibana  
- Grafana  
- Azure Application Insights  
- Jaeger  
- Elastic APM  

---

## 4. Security Integration

Franz depends on ASP.NET Core auth:

- OAuth2  
- OpenID Connect  
- JWT validation  
- mTLS (optional)  

Authorization flows are fully compatible with:

- Azure AD  
- Keycloak  
- ADFS  

---

## 5. Multi-Cloud Support

Franz services run seamlessly on:

- Azure Kubernetes Services  
- AWS ECS/EKS  
- GCP GKE  
- On-prem Kubernetes  
- VM-based clusters  

Messaging adapters integrate with:

- Azure Event Hubs  
- AWS SQS/SNS  
- On-prem Kafka clusters  

---

## 6. Summary

Franz integrates cleanly with modern cloud-native architectures and on-premise institutional environments, providing a stable foundation for large-scale modernization programs.
