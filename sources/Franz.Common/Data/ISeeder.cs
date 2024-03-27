using Franz.Common.DependencyInjection;

namespace Franz.Common.Data;

public interface ISeeder : IScopedDependency
{
  int Order { get; }

  void Seed();
}
