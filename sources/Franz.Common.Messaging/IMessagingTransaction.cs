namespace Franz.Common.Messaging;
public interface IMessagingTransaction
{
    void Begin();

    void Complete();

    void Rollback();
}
