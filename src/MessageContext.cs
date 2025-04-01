using Amqp;

namespace Korjn.AmqpMessageReceiver;

public sealed class MessageContext
{
    private readonly ReceiverLink receiver;

    internal MessageContext(Message message,
                            ReceiverLink receiver)
    {
        Message = message;
        this.receiver = receiver;
    }

    public Message Message { get; }

    public void Acknowledge(bool accept)
    {        
        if (receiver.IsClosed)
        {
            throw new InvalidOperationException("Receiver is closed. Message will be retried later.");
        }

        if (accept)
        {
            receiver.Accept(Message); // Подтверждаем обработку
        }
        else
        {
            receiver.Modify(Message, true, false); // Повторная доставка
        }
    }
}
