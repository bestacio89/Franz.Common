# Franz.Common Samples

> Practical examples for composing Franz.Common capabilities in .NET 10 applications.

## Purpose

Samples should demonstrate the framework's architectural philosophy: explicit composition, modular infrastructure, predictable behavior, and clear boundaries between domain code and external systems.

## How to use these samples

1. Read the sample's prerequisites and configuration before running it.
2. Supply credentials and connection strings through local configuration or environment variables; never commit secrets.
3. Run the sample with the repository-pinned .NET SDK and validate the behavior relevant to your infrastructure.
4. Treat sample code as an illustration of a pattern, not as a substitute for production security, resilience, and observability review.

## Sample-specific instructions

# Sample Implementations

The Franz.Common repository does not embed full microservices in order to maintain clean separation between:

- framework APIs and behaviors
- application templates
- concrete reference implementations

For a complete, ready-to-deploy sample microservice using Franz.Common, see:

👉 **Franz API Template**  
URL: https://github.com/bestacio89/Franz

This template demonstrates:

- HTTP pipelines  
- CQRS handlers  
- validation  
- logging + correlation  
- DI and module organization  
- structured folder conventions  
- resilience patterns  
- telemetry setup  

It is the canonical reference implementation for Franz.Common–based services.
