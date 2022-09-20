using FastEndpoints;
using FastEndpoints.Swagger;
using Flownodes.Edge.ApiGateway.Services;
using Orleans;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFastEndpoints();
builder.Services.AddSwaggerDoc();

builder.Services.AddSingleton<ClusterClientService>();
builder.Services.AddSingleton<IHostedService>(sp => sp.GetService<ClusterClientService>()!);
builder.Services.AddSingleton(sp => sp.GetService<ClusterClientService>()!.Client);
builder.Services.AddSingleton<IGrainFactory>(sp => sp.GetService<ClusterClientService>()!.Client);
builder.Services.AddSingleton<IEdgeService, EdgeService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi3(s => s.ConfigureDefaults());
}

app.UseHttpsRedirection();

app.UseAuthorization();
app.UseFastEndpoints();


app.Run();