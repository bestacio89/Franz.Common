namespace Franz.Common.Business.Domain;

/// <summary>Defines the audit lifecycle required by persistence entities.</summary>
public interface IAuditableEntity
{
    void MarkCreated(string createdBy);
    void MarkUpdated(string modifiedBy);
    void MarkDeleted(string deletedBy);
}
