using Carter;
using Flownodes.ApiGateway.Mediator.Requests;
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
            options.ConnectionString = builder.Configuration.GetConnectionString("redis") ?? "localhost:6379";
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
builder.Services.AddMediatR(config => { config.RegisterServicesFromAssembly(typeof(GetTenantRequest).Assembly); });
builder.Services.AddCarter();

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapCarter();
app.Run();