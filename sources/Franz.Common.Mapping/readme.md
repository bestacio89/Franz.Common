# **Franz.Common.Mapping**

*A deterministic, high-performance object mapping engine for the Franz Framework.*

* **Current Version:** v2.3.0
* **Codename:** *Mapping Engine Formalization & Constructor-Aware Evolution*
* Part of the **Franz Core Infrastructure Suite**

---

## 🌐 Overview

`Franz.Common.Mapping` is a **deterministic object mapping engine**, designed to replace traditional “magic mappers” with a **strict, explicit execution model**.

It is built around a single principle:

> Mapping is not inference — it is execution over declared intent.

This version formalizes the engine into a **three-layer architecture**:

* **Configuration Layer** → mapping intent (profiles & expressions)
* **Execution Engine** → deterministic transformation (FranzMapper)
* **Service Layer** → DI-safe orchestration (MappingService)

---

## 🧠 Core Architectural Upgrade (v2.2.2)

### 🔹 Mapping Engine Formalization

* Reframed the system into a **deterministic mapping execution pipeline**
* Removed ambiguity from implicit mapping behavior
* Standardized execution order across all mappings

### Execution Pipeline Order:

1. Circular reference detection (graph safety guard)
2. Value-object unwrapping (`Value` pattern resolution)
3. Collection mapping (generic enumerable handling)
4. Configured mappings (MappingConfiguration lookup)
5. Constructor-based projection (`ConstructUsing`)
6. Property-based fallback mapping

---

## 🏗 Architecture Separation

### 1. Configuration Layer (Mapping Intent)

* All mappings are explicitly registered
* No automatic or implicit discovery of mappings
* Deterministic override behavior: **last registration wins**

```csharp
config.Register(new MappingExpression<User, UserDto>()
    .ConstructUsing(u => new UserDto { Name = u.Name }));
```

---

### 2. Execution Engine (FranzMapper)

The `FranzMapper` is a **pure execution engine** responsible for applying mapping rules.

#### Key Responsibilities:

* Graph traversal and safety control
* Constructor resolution (including record types)
* Property binding execution
* Collection transformation
* Value-object unwrapping
* Fallback mapping logic

---

## 🧱 Constructor-Aware Mapping (NEW in v2.2.2)

### 🔹 Record & Immutable Type Support

The engine now natively supports:

* C# records (positional constructors)
* Immutable DTOs
* Constructor-only types

### Behavior:

* Automatically detects the **best constructor**
* Binds constructor parameters from source properties
* Supports `ConstructUsing` overrides
* Falls back to reflection-based instantiation only when necessary

```csharp
CreateMap<User, UserDto>()
    .ConstructUsing(u => new UserDto(u.Name, u.Email));
```

### Architectural Impact:

* Eliminates dependency on parameterless constructors
* Enables fully immutable DTO designs
* Aligns mapping with domain-driven design principles

---

## 🔁 Value Object Unwrapping System

### New deterministic unwrapping model:

The engine automatically resolves wrapped values:

```csharp
class WrappedInt
{
    public int Value { get; set; }
}
```

Mapping:

```csharp
WrappedInt → int
```

### Rules:

* Extracts `Value` property automatically
* Applies scalar conversion rules before mapping
* Preserves type safety when compatible
* Falls back to full mapping engine if needed

---

## 📦 Collection Mapping Engine

* Fully supports:

  * Lists
  * Enumerables
  * Arrays
* Element mapping is recursive and pipeline-consistent
* Uses cached delegate dispatch for performance optimization

---

## 🔄 Circular Reference Protection

* Built-in graph traversal tracking
* Prevents infinite recursion in object graphs
* Uses reference-equality tracking for accuracy

### Behavior:

```text
TechnicalException: Circular mapping detected
```

---

## ⚙️ Performance Model

The framework is optimized for:

* Minimal reflection in hot paths
* Cached constructor and property resolution
* Compiled delegate dispatch for recursive mappings
* Allocation-efficient collection handling

---

## 🧩 Mapping Service (DI Layer)

A lightweight orchestration layer over the engine.

### Responsibilities:

* Dependency injection integration
* Safe lifecycle management
* Async-compatible API surface

```csharp
var dto = service.Map<User, UserDto>(user);
```

### Async Behavior:

* Mapping remains **CPU-bound**
* Async API is a **compatibility and composition layer**
* No hidden concurrency introduced by default

---

## ⚖️ Design Guarantees

Franz Mapping guarantees:

* Deterministic output for identical inputs
* No hidden runtime discovery of mappings
* Thread-safe execution
* Strict separation of configuration and execution
* Predictable graph traversal semantics

---

## 🧠 Design Philosophy

> “Mapping is not inference. It is a deterministic execution graph over explicit intent.”

---

## 🚀 Version Highlights (v2.2.2)

### ✨ Major Changes

* Formalized mapping engine execution pipeline
* Introduced constructor-aware mapping (records supported natively)
* Strengthened value-object unwrapping system
* Standardized circular reference detection
* Unified mapping resolution order across all execution paths

### ⚙️ Internal Improvements

* Reduced reflection dependency in runtime mapping
* Improved constructor resolution logic
* Stabilized collection mapping pipeline
* Improved DI service consistency and lifecycle safety

---

