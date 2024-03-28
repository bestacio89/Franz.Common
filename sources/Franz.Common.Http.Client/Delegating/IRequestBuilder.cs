using Franz.Common.DependencyInjection;

namespace Franz.Common.Http.Client.Delegating;

public interface IRequestBuilder : IScopedDependency
{
  public bool CanBuild(HttpRequestMessage request);

  public void Build(HttpRequestMessage request);
}
