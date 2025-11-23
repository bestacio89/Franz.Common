# Reference Implementations

Franz.Common is used operationally inside a complete set of architecture templates, including:

- **Franz.Template.WebApi** – A full .NET service template with:
  - logging pipeline (Serilog + correlation IDs)
  - HTTP error middleware
  - CQRS/Mediator handlers
  - validation pipeline
  - tracing hooks
  - docker-compose integration
  - Azure/GCP/AWS ready configuration
  - Franz orchestration services pre-installed

Project URL:  
👉 https://github.com/bestacio89/Franz

This template is the recommended starting point for new projects adopting Franz.Common.
