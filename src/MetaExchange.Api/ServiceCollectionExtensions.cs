using MetaExchange.Core.Services;

namespace MetaExchange.Api;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    /// <exception cref="InvalidOperationException">Thrown when OrderBookFilePath is not configured.</exception>
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IExchangeService, FileExchangeService>(serviceProvider =>
        {
            var filePath = serviceProvider.GetRequiredService<IConfiguration>().GetValue<string>("OrderBookFilePath")
                           ?? throw new InvalidOperationException("OrderBookFilePath is not configured");
            return new FileExchangeService(filePath);
        });
        services.AddSingleton<IMetaExchangeService, MetaExchangeService>();

        return services;
    }
}