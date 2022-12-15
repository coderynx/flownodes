using Flownodes.Edge.ApiGateway;
using Flownodes.Edge.Core;
using Microsoft.AspNetCore.Mvc;
using Orleans.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOrleansClient(clientBuilder =>
{
    clientBuilder
        .Configure<ClusterOptions>(options =>
        {
            options.ClusterId = Environment.GetEnvironmentVariable("ORLEANS_CLUSTER_ID") ?? "dev";
            options.ServiceId = Environment.GetEnvironmentVariable("ORLEANS_SERVICE_ID") ?? "flownodes";
        })
        .UseRedisClustering(options =>
        {
            options.ConnectionString = builder.Configuration.GetConnectionString("redis");
            options.Database = 0;
        })
        .UseConnectionRetryFilter(async (_, token) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(4), token);
            return true;
        });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();
builder.Services.Configure<EnvironmentOptions>(options =>
{
    options.AlertManagerName = Environment.GetEnvironmentVariable("ALERT_MANAGER_NAME") ?? "alert_manager";
    options.ResourceManagerName = Environment.GetEnvironmentVariable("RESOURCE_MANAGER_NAME") ?? "resource_manager";
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapGet("/api/cluster", async ([FromServices] IClusterClient clusterClient) =>
{
    var grain = clusterClient.GetGrain<IClusterGrain>(0);
    return await grain.GetClusterInformation();
});

app.Run();