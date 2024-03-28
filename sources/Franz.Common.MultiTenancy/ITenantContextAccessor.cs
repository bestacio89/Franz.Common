namespace Franz.Common.MultiTenancy;

public interface ITenantContextAccessor
{
  Guid? GetCurrentId();
}
