namespace Franz.Common.Messaging.Sagas.Abstractions;

public interface ISagaStateWithGuid : ISagaState
{
  Guid? Id { get; set; }
}