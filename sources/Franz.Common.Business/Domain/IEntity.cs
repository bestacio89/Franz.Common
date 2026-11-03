namespace Franz.Common.Business.Domain;
public interface IEntity<TKey> : IAuditableEntity
{
  TKey Id { get; }
  object GetId();

  // Common properties for Query Filtering/Discovery
  bool IsDeleted { get; }
  DateTimeOffset DateCreated { get; }
}
