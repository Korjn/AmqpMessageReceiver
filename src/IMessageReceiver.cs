
using Korjn.AmqpClientInject;

namespace Korjn.AmqpMessageReceiver;

public interface IMessageReceiver
{
    Task ExecuteAsync(Func<MessageContext, Task> processMessage,
                      IAmqpClient client,
                      CancellationToken stoppingToken);
}
