# Contribution Guidelines – Franz.Common

This document explains how contributors (internal or external) should extend Franz.Common modules.

---

## 1. Principles

- Correctness over convenience  
- Predictability over cleverness  
- Simplicity over abstraction  
- Consistency across all modules  
- Backward-compatible changes preferred  

---

## 2. Adding New Modules

When creating a new module (e.g., `Franz.Common.Grpc`):

- follow naming conventions  
- place abstractions in `.Abstractions` folder  
- place provider-specific implementation in `.Providers.*` folders  
- ensure module has tests covering core behavior  
- document integration in `/docs`  

---

## 3. Extending Existing Modules

- Do not break existing behavior  
- If adding optional behaviors, disable by default  
- Follow mediator pipeline patterns  
- Document new extension methods  

---

## 4. Code Style

- C# 12+ features allowed  
- Avoid static state  
- Avoid highly nested structures  
- Prefer composition over inheritance  
- Async everywhere  

---

## 5. Testing

- All features must include:
  - unit tests  
  - integration tests if touching messaging/HTTP  
- No testing of private methods (test behaviors)  

---

## 6. Documentation

Every PR must include:

- updated changelog  
- updated docs if introducing new concepts  

---

## 7. Versioning

Follow the defined versioning policy:

- major = breaking  
- minor = features  
- patch = fixes  

---

## 8. Approval

- At least 1 senior reviewer  
- Reviewers validate:
  - architecture alignment  
  - backward compatibility  
  - code clarity  
  - performance considerations  

---

## 9. Summary

Contributions must strengthen the long-term maintainability and reliability of the Franz.Common ecosystem, not just deliver isolated features.
