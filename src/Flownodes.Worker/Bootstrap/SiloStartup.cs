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
    private readonly IExtensionProvider _extensionProvider;
    private readonly IServiceProvider _serviceProvider;

    public SiloStartup(IEnvironmentService environmentService, ILogger<SiloStartup> logger,
        IExtensionProvider extensionProvider, IHostEnvironment hostEnvironment, IServiceProvider serviceProvider)
    {
        _environmentService = environmentService;
        _logger = logger;
        _extensionProvider = extensionProvider;
        _hostEnvironment = hostEnvironment;
        _serviceProvider = serviceProvider;
    }

    public async Task Execute(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Flownodes node");

        _extensionProvider.BuildContainer();

        var userManager = _environmentService.GetUserManager();
        var seedDefaultUser = (_hostEnvironment.IsDevelopment() || _hostEnvironment.IsStaging()) &&
                              !await userManager.HasUsers();
        if (seedDefaultUser) await SeedDefaultUsers();

        _logger.LogInformation("Flownodes node successfully started");
    }

    private async Task SeedDefaultUsers()
    {
        using var scope = _serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            Email = "admin@flownodes.com",
            UserName = "admin"
        };
        await userManager.CreateAsync(user, "P@ssw0rd1");
    }
}