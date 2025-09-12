namespace Franz.Common.MultiTenancy;
#nullable enable  
public interface IDomainResolver
{
  int Order { get; }
  Task<DomainResolutionResult> ResolveDomainAsync(object? context = null);
}
