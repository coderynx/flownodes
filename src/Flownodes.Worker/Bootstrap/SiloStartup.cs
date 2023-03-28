using Flownodes.Shared.Authentication;
using Flownodes.Shared.Authentication.Models;
using Flownodes.Worker.Extendability;
using Flownodes.Worker.Services;
using Microsoft.AspNetCore.Identity;
using Orleans.Runtime;

namespace Flownodes.Worker.Bootstrap;

public class SiloStartup : IStartupTask
{
    private readonly IEnvironmentService _environmentService;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<SiloStartup> _logger;
    private readonly IComponentProvider _componentProvider;
    private readonly IServiceProvider _serviceProvider;

    public SiloStartup(IEnvironmentService environmentService, ILogger<SiloStartup> logger,
        IComponentProvider componentProvider, IHostEnvironment hostEnvironment, IServiceProvider serviceProvider)
    {
        _environmentService = environmentService;
        _logger = logger;
        _componentProvider = componentProvider;
        _hostEnvironment = hostEnvironment;
        _serviceProvider = serviceProvider;
    }

    public async Task Execute(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Flownodes node");

        _componentProvider.BuildContainer();

        var userManager = _environmentService.GetUserManager();
        var seedDefaultUser = (_hostEnvironment.IsDevelopment() || _hostEnvironment.IsStaging()) &&
                              !await userManager.HasUsers();
        if (seedDefaultUser) await SeedDefaultUserAndApiKey();

        _logger.LogInformation("Flownodes node successfully started");
    }

    private async Task<string> SeedDefaultUserAndApiKey()
    {
        using var scope = _serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var apiKeyManager = _environmentService.GetApiKeyManager();

        var user = new ApplicationUser
        {
            Email = "admin@flownodes.com",
            UserName = "admin"
        };
        await userManager.CreateAsync(user, "P@ssw0rd1");

        var apiKey = await apiKeyManager.GenerateApiKeyAsync("default", user.UserName);
        _logger.LogInformation("Seeded default user and default ApiKey");

        return apiKey;
    }
}