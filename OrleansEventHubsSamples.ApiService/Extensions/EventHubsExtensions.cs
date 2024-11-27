using Microsoft.Extensions.Options;
using Orleans.Configuration;

namespace Orleans.Hosting;

public static class EventHubsExtensions
{
    public static ISiloBuilder AddEventHubsStreams(
        this ISiloBuilder builder,
        string name,
        Action<OptionsBuilder<EventHubOptions>> configureOptions,
        Action<OptionsBuilder<AzureTableStreamCheckpointerOptions>> configureDefaultCheckpointer)
    {
        return builder.AddEventHubStreams(name, b =>
        {
            b.ConfigureEventHub(configureOptions);
            b.UseAzureTableCheckpointer(configureDefaultCheckpointer);
        });

        //return builder.ConfigureServices(
        //    services =>
        //    {
        //        var configurator = new SiloEventHubStreamConfigurator(
        //            name,
        //            configureServicesDelegate => builder.ConfigureServices(configureServicesDelegate)
        //        );

        //        configureOptions?.Invoke(services.AddOptions<EventHubOptions>());
        //    });
    }

    public static IClientBuilder AddEventHubsStreams(
        this IClientBuilder builder,
        string name,
        Action<OptionsBuilder<EventHubOptions>> configureOptions)
    {

        return builder.AddEventHubStreams(name, b =>
        {
            b.ConfigureEventHub(configureOptions);
            // b.UseAzureTableCheckpointer(ob => ob.Configure(configureDefaultCheckpointer));
        });

        //return builder.ConfigureServices(
        //    services =>
        //    {
        //        var configurator = new ClusterClientEventHubStreamConfigurator(name, builder);

        //        configureOptions?.Invoke(services.AddOptions<EventHubOptions>());
        //    });
    }
}