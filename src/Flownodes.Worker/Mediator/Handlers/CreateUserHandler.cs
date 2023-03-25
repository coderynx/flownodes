using Flownodes.Shared.Authentication.Models;
using Flownodes.Worker.Mediator.Requests;
using Flownodes.Worker.Mediator.Responses;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Flownodes.Worker.Mediator.Handlers;

public class CreateUserHandler : IRequestHandler<CreateUserRequest, CreateUserResponse>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateUserHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<CreateUserResponse> Handle(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            UserName = request.Username,
            Email = request.Email
        };
        var result = await _userManager.CreateAsync(user, request.Password);

        return result.Succeeded
            ? new CreateUserResponse(request.Username, request.Email)
            : new CreateUserResponse(request.Username, request.Email, result.ToString(), ResponseKind.BadRequest);
    }
}