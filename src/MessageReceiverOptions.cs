
namespace Korjn.AmqpMessageReceiver;

public class MessageReceiverOptions
{
    public string? ReceiverName { get; set; }
    public string? Address { get; set; }    
    public int MaxConcurrency { get; set; }
}
