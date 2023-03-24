using Flownodes.Shared.Users;
using Flownodes.Worker.Mediator.Requests;
using Flownodes.Worker.Mediator.Responses;
using Flownodes.Worker.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Flownodes.Worker.Mediator.Handlers;

public class CreateApiKeyHandler : IRequestHandler<CreateApiKeyRequest, CreateApiKeyResponse>
{
    private readonly IApiKeyManagerGrain _apiKeyManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateApiKeyHandler(IEnvironmentService environmentService, UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
        _apiKeyManager = environmentService.GetApiKeyManager();
    }

    public async Task<CreateApiKeyResponse> Handle(CreateApiKeyRequest request, CancellationToken cancellationToken)
    {
        if (await _userManager.FindByNameAsync(request.Username) is null)
        {
            return new CreateApiKeyResponse(request.Username, request.Name, "The user was not found",
                ResponseKind.NotFound);
        }

        try
        {
            var apiKey = await _apiKeyManager.GenerateApiKeyAsync(request.Name, request.Username);
            return new CreateApiKeyResponse(request.Username, request.Name, apiKey);
        }
        catch
        {
            return new CreateApiKeyResponse(request.Username, request.Name, "Could not create ApiKey",
                ResponseKind.InternalError);
        }
    }
}