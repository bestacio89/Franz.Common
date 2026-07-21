using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Mediator.Registration;

/// <summary>
/// Contract implemented by source-generated mediator registrations.
/// </summary>
public interface IGeneratedHandlerRegistration
{
  void Register(IServiceCollection services);
}