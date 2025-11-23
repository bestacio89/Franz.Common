# Audit Logging – Franz.Common

Audit logging is essential in institutional environments.

Franz.Common supports audit logging via:

- standardized logging scopes (correlation ID, user ID),
- consistent formatting of event logs,
- hooks in mediator behaviors and HTTP middleware.

Typical audited events:

- sensitive domain operations (create/update/delete),
- security-related actions (login, token validation failures),
- configuration changes.

Implementations:

- An `IAuditLogger` abstraction can be provided by consuming projects,
- Franz behaviors/enrichers call into `IAuditLogger` at key points.

Audit logs should be sent to a secure, immutable storage (e.g., append-only log, SIEM).
