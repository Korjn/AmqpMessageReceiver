using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Korjn.AmqpMessageReceiver.DependencyInjection;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAmqpMessageReceiver(this IServiceCollection services,                                                        
                                                            Action<MessageReceiverOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Configure(configureOptions);

        services.TryAddSingleton<IMessageReceiver, MessageReceiver>();

        return services;
    }    
}