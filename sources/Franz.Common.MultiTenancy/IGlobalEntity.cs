namespace Franz.Common.MultiTenancy;

/// <summary>
/// Marks a persistence entity as intentionally shared across tenants.
/// Use this marker only for data that is safe and valid to expose across tenant boundaries.
/// </summary>
public interface IGlobalEntity
{
}
