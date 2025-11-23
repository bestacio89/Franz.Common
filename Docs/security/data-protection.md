# Data Protection & Privacy – Franz.Common

This document describes how Franz.Common assists in building GDPR-compliant systems.

---

## 1. PII Handling

Franz encourages:

- avoiding logging raw PII  
- redacting sensitive fields  
- providing structured logs  
- configuring Serilog destructuring policies  

Sensitive data must be masked using:
```csharp
Log.ForContext("email", Mask(email));
```

### 2. Data Minimization

- Franz modules store no business data.

- Any PII processed by applications is:

- transient

- bound to application-level handlers

- not persisted by Franz itself

3. Encryption

- Applications should use:

- HTTPS everywhere

- Encrypted connection strings

- Encrypted message payloads (optional)

- Disk encryption on services

- Franz integrates with any standard ASP.NET Core crypto provider.

### 4. Right-to-Erasure Support

Franz best practices:

- domain events for deletion

- soft delete tracking

- propagating deletion requests through messaging

### 5. Audit Logs and Compliance

Audit logs must not contain:

- passwords

- tokens

- sensitive documents

- unnecessary PII

- Audit logs may include IDs (GUIDs, hashed identifiers).

### 6. Summary

Franz provides the tools for secure and compliant systems, but applications must implement domain-specific privacy requirements.
