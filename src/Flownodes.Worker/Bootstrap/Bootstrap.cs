using System.Text.Json;
using System.Text.Json.Serialization;
using Carter;
using Flownodes.Shared.Authentication.Models;
using Flownodes.Worker.Authentication;
using Flownodes.Worker.Authentication.Stores;
using Flownodes.Worker.Extendability;
using Flownodes.Worker.Mediator.Requests;
using Flownodes.Worker.Services;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;
using Orleans.Configuration;
using Serilog;
using StackExchange.Redis;

namespace Flownodes.Worker.Bootstrap;

internal static class Bootstrap
{
    public static void ConfigureWebServices(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Description = "Flownodes API to manage the cluster",
                Version = "v1",
                Title = "Flownodes API"
            });
            options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Description = "The API key to access the cluster API",
                Type = SecuritySchemeType.ApiKey,
                Name = "X-Api-Key",
                In = ParameterLocation.Header,
                Scheme = "ApiKeyScheme"
            });
            var scheme = new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                },
                In = ParameterLocation.Header
            };
            var requirement = new OpenApiSecurityRequirement
            {
                { scheme, new List<string>() }
            };
            options.AddSecurityRequirement(requirement);
        });

        services.AddIdentity<ApplicationUser, ApplicationRole>()
            .AddUserStore<GrainUserStore>()
            .AddRoleStore<GrainRoleClaimStore>()
            .AddDefaultTokenProviders();

        services.Configure<IdentityOptions>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false;
            options.SignIn.RequireConfirmedEmail = false;
            options.SignIn.RequireConfirmedPhoneNumber = false;
        });

        services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        });

        services.AddAuthentication();
        services.AddAuthorization();
        services.AddMediatR(config => { config.RegisterServicesFromAssembly(typeof(GetTenantRequest).Assembly); });
        services.AddCarter();
    }

    private static void ConfigureOrleansServices(this IServiceCollection services)
    {
        services.AddOptions();
        services.AddSingleton<IComponentProvider, ComponentProvider>();
        services.AddSingleton<IEnvironmentService, EnvironmentService>();
        services.AddHostedService<TestWorker>();
    }

    private static void ConfigureDevelopment(this ISiloBuilder builder)
    {
        builder.AddMemoryGrainStorageAsDefault()
            .UseLocalhostClustering()
            .UseDashboard();
    }

    private static void ConfigureProduction(this ISiloBuilder builder, HostBuilderContext context)
    {
        var redisConnectionString = EnvironmentVariables.RedisConnectionString
                                    ?? context.Configuration.GetConnectionString("redis")
                                    ?? "localhost:6379";

        builder
            .UseRedisClustering(options =>
            {
                options.ConfigurationOptions = ConfigurationOptions.Parse(redisConnectionString);
            })
            .AddRedisGrainStorageAsDefault(options =>
            {
                options.ConfigurationOptions = ConfigurationOptions.Parse(redisConnectionString);
                options.DeleteStateOnClear = true;
            });
    }

    public static void ConfigureOrleans(this IHostBuilder hostBuilder)
    {
        hostBuilder.UseOrleans((context, siloBuilder) =>
        {
            siloBuilder.Services.ConfigureOrleansServices();

            siloBuilder.ConfigureEndpoints(
                EnvironmentVariables.OrleansSiloPort ?? 11111,
                EnvironmentVariables.OrleansGatewayPort ?? 30000
            );

            siloBuilder.Configure<ClusterOptions>(options =>
            {
                options.ClusterId = EnvironmentVariables.OrleansClusterId ?? "dev";
                options.ServiceId = EnvironmentVariables.OrleansServiceId ?? "flownodes";
            });

            siloBuilder.AddLogStorageBasedLogConsistencyProviderAsDefault();
            siloBuilder.AddStartupTask<SiloStartup>();

            if (context.HostingEnvironment.IsDevelopment())
            {
                siloBuilder.Configure<AdminSecret>(_ => { });
                siloBuilder.ConfigureDevelopment();
            }

            if (!context.HostingEnvironment.IsStaging() && !context.HostingEnvironment.IsProduction()) return;
            
            siloBuilder.Configure<AdminSecret>(config =>
            {
                config.Secret = EnvironmentVariables.AdminSecret ??
                                throw new InvalidOperationException("Admin secret not provided");
            });
            siloBuilder.ConfigureProduction(context);
        });
    }

    public static void ConfigureSerilog(this IHostBuilder builder)
    {
        builder.UseSerilog((context, configuration) =>
        {
            configuration.ReadFrom.Configuration(context.Configuration).Enrich.FromLogContext();
        });
    }
}