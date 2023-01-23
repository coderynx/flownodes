using Flownodes.Waikiki.Services;
using MudBlazor.Services;
using Orleans.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();
builder.Services.AddOrleansClient(options =>
{
    options
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

builder.Services.AddSingleton<IContextService, ContextService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();