using Franz.Common.Aras.Abstractions.Contexts.Contracts;
using Franz.Common.Business;
using Franz.Common.Business.Domain;
using Franz.Common.Mediator.Dispatchers;

public interface IArasContext : IArasEntityContext, IArasAggregateContext
{
}
