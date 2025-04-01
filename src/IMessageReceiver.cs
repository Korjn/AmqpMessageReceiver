
using Korjn.AmqpClientInject;

namespace Korjn.AmqpMessageReceiver;

public interface IMessageReceiver
{
    Task ExecuteAsync(IAmqpClient client,
                      Func<MessageContext, Task> processMessage,
                      CancellationToken stoppingToken);
}
