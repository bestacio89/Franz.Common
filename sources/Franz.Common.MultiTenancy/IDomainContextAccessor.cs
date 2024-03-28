namespace Franz.Common.MultiTenancy;

public interface IDomainContextAccessor
{
  Guid? GetCurrentId();
}
