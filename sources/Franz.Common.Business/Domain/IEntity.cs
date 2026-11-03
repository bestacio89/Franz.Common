namespace Franz.Common.Business.Domain;
public interface IEntity<TKey> : IAuditableEntity
{
  TKey Id { get; }
  object GetId();

  // Preserve the lifecycle members on IEntity for reflection-based consumers.
  new void MarkCreated(string createdBy);
  new void MarkUpdated(string modifiedBy);
  new void MarkDeleted(string deletedBy);

  // Common properties for Query Filtering/Discovery
  bool IsDeleted { get; }
  DateTimeOffset DateCreated { get; }
}
