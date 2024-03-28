using Franz.Common.DependencyInjection;

namespace Franz.Common.Hosting;

public interface IHostingInitializer : IScopedDependency
{
  int Order { get; }

  void Initialize();
}
