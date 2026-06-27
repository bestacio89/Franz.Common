namespace Franz.Common.Business.Domain;
public interface IEntity<TKey>
{
  TKey Id { get; }
  object GetId();

  // Technical Railguards for Auditing & Persistence
  void MarkCreated(string createdBy);
  void MarkUpdated(string modifiedBy);
  void MarkDeleted(string deletedBy);

  // Common properties for Query Filtering/Discovery
  bool IsDeleted { get; }
  DateTimeOffset DateCreated { get; }
}
