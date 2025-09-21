using Franz.Common.Mapping.Abstractions;
using Franz.Common.Mapping.Core;

public abstract class FranzMapProfile : IFranzMapProfile
{
  private readonly List<Action<MappingConfiguration>> _registrations = new();

  protected MappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>()
  {
    var expr = new MappingExpression<TSource, TDestination>();
    _registrations.Add(cfg => cfg.Register(expr));
    return expr;
  }

  public void Configure(MappingConfiguration config)
  {
    foreach (var reg in _registrations)
      reg(config);
  }
}
