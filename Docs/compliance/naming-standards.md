# Naming Standards – Franz.Common

Applies to internal code and consuming services.

---

## 1. Commands & Queries

CreateOrderCommand
GetOrderByIdQuery

yaml
Copy code

Suffixes required:
- `Command`
- `Query`
- `Event`
- `Handler`

---

## 2. DTOs
## 2. DTOs

OrderDto
CustomerDto

yaml
Copy code

Immutable DTOs recommended.

---

## 3. Events

OrderCreatedEvent
PaymentAuthorizedEvent

yaml
Copy code

Events must be past tense.

---

## 4. Modules

Franz.Common.Http
Franz.Common.Messaging.Kafka
Franz.Common.Validation

---

## 5. Folders

Application/
Domain/
Infrastructure/
Controllers/

yaml
Copy code

---

## 6. Summary

Naming rules maintain clarity and consistency across all teams, services, and modules.