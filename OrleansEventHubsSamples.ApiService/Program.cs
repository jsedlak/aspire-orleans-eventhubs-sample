using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Amqp.Encoding;
using Microsoft.VisualBasic;
using OrleansEventHubsSamples.ApiService.Grains;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Grab some config
var checkpointerConnectionString = builder.Configuration.GetConnectionString("checkpointerStorage");

// Grab our providers
builder.AddKeyedAzureTableClient("clustering");
builder.AddKeyedAzureBlobClient("grain-state");
builder.AddKeyedAzureBlobClient("pubsub");
builder.AddKeyedAzureTableClient("eventhubs", s => s.ConnectionString = checkpointerConnectionString);

//builder.AddKeyedAzureEventHubProducerClient("streaming");
//builder.AddKeyedAzureEventHubConsumerClient("streaming");

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

Console.WriteLine(
    builder.Configuration.GetDebugView()
);

//var eventHubsConnectionString = builder.Configuration.GetConnectionString("eventhubs");
//var eventHubsPath = "events";
//var eventHubsConsumerGroup = "silo";

// Add Microsoft Orleans
builder.UseOrleans();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/api/account/{accountId}", async ([FromServices]IClusterClient clusterClient, [FromRoute]int accountId) =>
{
    var grain = clusterClient.GetGrain<IAccountGrain>(accountId);
    var balance = await grain.GetBalance();

    return new
    {
        balance
    };
})
.WithName("GetAccountBalance");

app.MapPost("/api/account/{accountId}/deposit", async ([FromServices] IClusterClient clusterClient, [FromRoute] int accountId, [FromBody]AmountModel model) =>
{
    var grain = clusterClient.GetGrain<IAccountGrain>(accountId);
    var balance = await grain.Deposit(model.Amount);

    return new
    {
        balance
    };
})
.WithName("DepositToAccount");

app.MapPost("/api/account/{accountId}/withdraw", async ([FromServices] IClusterClient clusterClient, [FromRoute] int accountId, [FromBody] AmountModel model) =>
{
    var grain = clusterClient.GetGrain<IAccountGrain>(accountId);
    var amountWithdrawn = await grain.Withdraw(model.Amount);

    return new
    {
        amountWithdrawn
    };
})
.WithName("WithdrawFromAccount");

app.MapDefaultEndpoints();

app.Run();

internal class AmountModel
{
    public double Amount { get; set; }
}