using Franz.Common.DependencyInjection.Properties;

using Microsoft.Extensions.DependencyInjection;

using Scrutor;

namespace Franz.Common.DependencyInjection;

public class RegistrationStrategySkipExistingPair : RegistrationStrategy
{
    public override void Apply(IServiceCollection services, ServiceDescriptor descriptor)
    {
        var currentDescriptor = services
    .FirstOrDefault(service => service.ImplementationType == descriptor.ImplementationType &&
      service.ServiceType == descriptor.ServiceType);

        if (currentDescriptor != null && currentDescriptor.Lifetime != descriptor.Lifetime)
            throw new InvalidOperationException(string.Format(Resources.MismatchLifetimeServiceException, currentDescriptor.ServiceType, currentDescriptor.ImplementationType));

        if (currentDescriptor == null)
            services.Add(descriptor);
    }
}
