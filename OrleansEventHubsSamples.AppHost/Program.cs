using Aspire.Hosting.Azure;
using System.Text.Json;

var builder = DistributedApplication.CreateBuilder(args);

var eventHubs = builder.AddAzureEventHubs("eventhubs")
    .AddEventHub("events")
    .RunAsEmulator();

// workaround from: https://gist.github.com/oising/3dd68b7605cae511434ced4971b6551a
// relevant: https://github.com/dotnet/aspire/issues/5561
builder.Eventing.Subscribe<AfterEndpointsAllocatedEvent>(
    async (e, ct) =>
    {
        var emulatorResource = builder.Resources
            .OfType<AzureEventHubsResource>().Single(x => x is
            {
                IsEmulator: true
            });
        string targetPath = "/Eventhubs_Emulator/ConfigFiles/Config.json";
        var configFileMount = emulatorResource.Annotations.OfType<ContainerMountAnnotation>()
                .Single(v => v.Target == targetPath);
        var json = await File.ReadAllTextAsync(configFileMount.Source!);
        var config = JsonSerializer.Deserialize<EmulatorConfig>(json);
        var hub = config!.UserConfig.NamespaceConfig[0].Entities[0];

        hub.PartitionCount = "4";
        // note: do NOT add $default - it will be added automatically by the emulator
        hub.ConsumerGroups = [
            new() {  Name="silo" },
            new() { Name = "bar" }];
        await using var s = File.OpenWrite(configFileMount.Source!);
        JsonSerializer.Serialize(s, config);
    });

/* Our Orleans Cluster & API */
var storage = builder.AddAzureStorage("cluster")
    .RunAsEmulator(r => r.WithImage("azure-storage/azurite", "3.33.0"));

var grainStorage = storage.AddBlobs("grain-state");
var pubSubStorage = storage.AddBlobs("pubsub");
var clusteringStorage = storage.AddTables("clustering");
var checkpointerStorage = storage.AddTables("checkpointerStorage");

// Add Orleans
var orleans = builder.AddOrleans("orleans")
    //.WithClustering(clusteringStorage)
    .WithDevelopmentClustering()
    .WithGrainStorage("PubSubStore", pubSubStorage)
    .WithGrainStorage("Default", grainStorage)
    .WithStreaming("StreamProvider", eventHubs);

var apiService = builder
    .AddProject<Projects.OrleansEventHubsSamples_ApiService>("silo")
    .WithReference(eventHubs)
    .WithReference(orleans)
    .WithReference(grainStorage)
    .WithReference(pubSubStorage)
    .WithReference(clusteringStorage)
    .WithReference(checkpointerStorage)
    .WaitFor(storage)
    .WaitFor(eventHubs);

builder.Build().Run();

public class EmulatorConfig
{
    public UserConfig UserConfig { get; set; }
}

public class UserConfig
{
    public List<NamespaceConfig> NamespaceConfig { get; set; }
    public LoggingConfig LoggingConfig { get; set; }
}

public class LoggingConfig
{
    public string Type { get; set; }
}

public class NamespaceConfig
{
    public string Type { get; set; }
    public string Name { get; set; }
    public List<Entity> Entities { get; set; }
}

public class Entity
{
    public string Name { get; set; }
    public string PartitionCount { get; set; }
    public List<ConsumerGroup> ConsumerGroups { get; set; }
}

public class ConsumerGroup
{
    public string Name { get; set; }
}