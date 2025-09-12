namespace Franz.Common.MultiTenancy;

public interface ITenantContextAccessor
{
  Guid? GetCurrentTenantId();
  void SetCurrentTenantId(Guid tenantId);
}
