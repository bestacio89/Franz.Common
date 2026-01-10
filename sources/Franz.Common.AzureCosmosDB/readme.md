Here is a **clean, polished, professional** update to your internal README for **Franz.Common.AzureCosmosDB**, updated to **Version 1.7.5**, and incorporating ALL the new changes you introduced in Franz 1.7.x.

I’ve rewritten only what needed updating, preserved your structure, and aligned it with your OSS-quality documentation standard.

---

# **Franz.Common.AzureCosmosDB (v1.7.5)**

A specialized persistence library within the **Franz Framework**, providing seamless integration with **Azure Cosmos DB**.

This package extends the framework’s **polyglot persistence** capabilities by enabling:

* **NoSQL document storage**
* **Outbox pattern messaging**
* **Dead-letter storage**
* **Cosmos-backed distributed transaction patterns**
* **EF Core Cosmos provider alignment**

All designed following Franz’s *deterministic, message-driven architecture*.

---

## **Features**

### 🚀 **Cosmos DB Bootstrapping**

* High-level `ServiceCollectionExtensions` for Cosmos initialization.
* Automatic provisioning of:

  * `CosmosClient`
  * `Database`
  * Containers with partitioning
* Supports typed container resolution through configuration patterns.

---

### 📬 **Outbox & Dead-Letter Messaging**

* `CosmosDBMessageStore` implements `IMessageStore` from **Franz.Common.Messaging.Storage**.
* Guarantees:

  * Async-safe writes
  * Deterministic message IDs
  * Automatic dead-lettering
  * Resilient retry-based delivery

Now supports **Batch Writes** introduced in 1.7.5 for improved performance.

---

### 📦 **Repository Support**

A generic Cosmos repository abstraction:

```csharp
ICosmosRepository<T>
```

providing:

* CRUD operations
* Partition key awareness
* Automatic model-to-container mapping
* Optional optimistic concurrency

Now aligned with the **Cosmos EF Provider** conventions for maximum portability across SQL, Mongo, and Cosmos stores.

---

### 🔧 **EF Core Cosmos Provider (1.7.x Integration)**

Franz 1.7.x unifies Cosmos EF Core support:

* New `CosmosDbContextBase`
* Conventions via `ApplyCosmosConventions()`
* Default container fallback for multi-container apps
* Deterministic outbox dispatch integration using `IDispatcher`

This makes CosmosDB a first-class citizen in the event-sourced pipeline.

---

### ⚡ **Version 1.7.5 Additions**

#### **Major Additions**

* Full alignment with `.NET 10.0` SDK.
* Cosmos EF provider stabilization & conventions.
* Unified Cosmos document serializer aligned with Franz’s null-safety rules.
* Deterministic container creation logic (optional auto-provisioning).
* Batch persistence for message-based storage.

#### **Fixes & Improvements**

* Hardened message serialization for CosmosDB.
* Fixed dead-letter partition routing.
* Improved DI boot ordering for Cosmos-backed message stores.
* Refactored container naming strategy for multi-tenant scenarios.
* Ensured async safety across the entire Cosmos subsystem.

---

## **Installation**

### **From Private Azure Feed**

```bash
dotnet nuget add source "https://your-private-feed-url" \
  --name "AzurePrivateFeed" \
  --username "YourAzureUsername" \
  --password "YourAzurePassword" \
  --store-password-in-clear-text
```

Install:

```bash
dotnet add package Franz.Common.AzureCosmosDB
```

---

## **Usage**

### 1️⃣ **Configure Cosmos DB**

```json
{
  "CosmosDb": {
    "ConnectionString": "...",
    "DatabaseName": "FranzAppDb",
    "Containers": {
      "Messages": "outbox",
      "DeadLetters": "failed"
    }
  }
}
```

---

### 2️⃣ **Register Cosmos in DI**

```csharp
services.AddCosmosDatabase(Configuration);

services.AddCosmosMessageStore(
    Configuration["CosmosDb:ConnectionString"],
    Configuration["CosmosDb:DatabaseName"]);
```

---

### 3️⃣ **Using the Message Store**

```csharp
var msg = new Message("hello");
await messageStore.SaveAsync(msg);
```

---

## **Dependencies**

* **Franz.Common.Messaging**
* **Franz.Common.Messaging.Storage**
* **Microsoft.Azure.Cosmos**
* **Franz.Common.EntityFramework** (optional for EF provider)

---

## **Changelog**

### **Version 1.7.5**

🔹 CosmosDB EF provider fully integrated
🔹 Unified Cosmos conventions for container naming & partitioning
🔹 Batch write support for outbox
🔹 Deterministic message serialization & schema validation
🔹 Improved DI bootstrapping order
🔹 Full .NET 10 alignment

### **Version 1.6.2**

* Introduced Cosmos DB integration.
* Added `CosmosDBMessageStore`.
* Added generic repository pattern.
* Added outbox/dead-letter support.

### **Version 1.6.20**

* Updated to .NET 10 SDK.

---

## **Contributing**

Internal to Franz Framework development team.

---

## **License**

MIT

---


