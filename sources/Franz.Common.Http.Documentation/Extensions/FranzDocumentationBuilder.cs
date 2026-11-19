using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Http.Documentation.Extensions;

public sealed class FranzDocumentationBuilder
{
  internal IServiceCollection Services { get; }

  internal FranzDocumentationBuilder(IServiceCollection services)
  {
    Services = services;
  }
}
