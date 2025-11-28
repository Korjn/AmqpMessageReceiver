using System.Threading.Channels;
using Amqp;
using Korjn.AmqpClientInject;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Korjn.AmqpMessageReceiver;

internal class MessageReceiver(ILogger<IAmqpClient> logger, IOptions<MessageReceiverOptions> _options) : IMessageReceiver
{
    private readonly MessageReceiverOptions options = _options.Value;

    private async Task ConsumeAsync(ChannelReader<MessageContext> reader,
                                    Func<MessageContext, Task> processMessage,
                                    CancellationToken stoppingToken)
    {
        logger.LogTrace("Start consuming from channel {task}", Environment.CurrentManagedThreadId);

        await foreach (var context in reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await processMessage(context);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Process Message");
            }
            finally
            {
                context.Message?.Dispose();
            }
        }
    }

    private async Task DoReceiveAsync(ChannelWriter<MessageContext> writer, ReceiverLink receiver, CancellationToken stoppingToken)
    {
        var timeOut = TimeSpan.FromSeconds(10);

        while (!stoppingToken.IsCancellationRequested)
        {
            var receiveTask = receiver.ReceiveAsync(timeOut);
            var delayTask = Task.Delay(timeOut, stoppingToken); // Отменяемый `Task.Delay`            

            var completedTask = await Task.WhenAny(receiveTask, delayTask);

            if (completedTask == delayTask)
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    // `stoppingToken` сработал
                    logger.LogWarning("Message receiving was cancelled.");

                    break; // Выходим из цикла
                }

                // `timeOut` вышел
                logger.LogTrace("No messages received, waiting 1 sec...");

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

                continue;
            }

            var message = await receiveTask;

            if (message != null)
            {
                // Передаем сообщение в канал                
                await writer.WriteAsync(new MessageContext(message, receiver), stoppingToken);
            }
        }
    }

    private async Task ReceiveLoopAsync(ChannelWriter<MessageContext> writer,
                                        IAmqpClient client,
                                        CancellationToken stoppingToken)
    {
        logger.LogTrace("Start message receive from {address}", options.Address);


        var maxDelay = TimeSpan.FromSeconds(10);
        var currentDelay = TimeSpan.FromSeconds(1);
        ReceiverLink? receiver = default;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                receiver = await client.CreateReceiverAsync(options.ReceiverName!, options.Address!);

                currentDelay = TimeSpan.FromSeconds(1);

                await DoReceiveAsync(writer, receiver, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogTrace("Message receiving loop was cancelled.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in message receive loop. Retrying in {delay}...", currentDelay);
                await Task.Delay(currentDelay, stoppingToken);
                currentDelay = TimeSpan.FromMilliseconds(Math.Min(currentDelay.TotalMilliseconds * 2, maxDelay.TotalMilliseconds));
            }
            finally
            {
                if (receiver != null)
                    try
                    {
                        await receiver.CloseAsync();
                    }
                    catch (Exception closeEx)
                    {
                        logger.LogWarning(closeEx, "Failed to close receiver");
                    }
            }
        }

        // Завершаем канал
        writer.Complete();
    }

    public async Task ExecuteAsync(IAmqpClient client,
                                   Func<MessageContext, Task> processMessage,                                   
                                   CancellationToken stoppingToken)
    {
        // Проверяем, заданы ли параметры

        ArgumentException.ThrowIfNullOrEmpty(options.ReceiverName, nameof(options.ReceiverName));
        ArgumentException.ThrowIfNullOrEmpty(options.Address, nameof(options.Address));

        // Создаем канал с ограниченной очередью
        var channelOptions = new BoundedChannelOptions(options.MaxConcurrency)
        {
            SingleWriter = true,
            SingleReader = false,
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.Wait // Блокирует запись, если очередь полная
        };

        var queue = Channel.CreateBounded<MessageContext>(channelOptions);

        // Запускаем обработчики сообщений в нескольких потоках
        var consumeTasks = Enumerable.Range(0, options.MaxConcurrency)
                                     .Select(_ => ConsumeAsync(queue.Reader, processMessage, stoppingToken))
                                     .ToList();

        // Запускаем процесс получения сообщени
        await ReceiveLoopAsync(queue.Writer, client, stoppingToken);

        // Дожидаемся завершения всех обработчиков
        await Task.WhenAll(consumeTasks);
    }
}