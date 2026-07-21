using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Mediator.Registration;

/// <summary>
/// Provides handler registrations for Franz Mediator.
/// Implementations can use reflection, generated code, or other discovery mechanisms.
/// </summary>
public interface IHandlerRegistrationProvider
{
  void Register(IServiceCollection services);
}