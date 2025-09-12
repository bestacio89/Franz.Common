namespace Franz.Common.MultiTenancy;

public interface IDomainContextAccessor
{
  Guid? GetCurrentDomainId();
  void SetCurrentDomainId(Guid domainId);
}
