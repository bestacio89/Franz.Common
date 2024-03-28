namespace Franz.Common.Messaging.Factories;

public interface IMessageFactory
{
    Message Build(object value);
}
