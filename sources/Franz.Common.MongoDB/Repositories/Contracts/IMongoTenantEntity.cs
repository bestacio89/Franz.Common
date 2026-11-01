using Franz.Common.MultiTenancy;

namespace Franz.Common.MongoDB.Repositories.Contracts;

/// <summary>
/// MongoDB document contract for records owned by one Franz tenant.
/// MongoDB uses a native document field and compound indexes rather than EF query filters.
/// </summary>
public interface IMongoTenantEntity : ITenantEntity
{
    string Id { get; set; }
}
