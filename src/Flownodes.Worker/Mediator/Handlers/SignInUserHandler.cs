using Flownodes.Shared.Authentication.Models;
using Flownodes.Worker.Mediator.Requests;
using Flownodes.Worker.Mediator.Responses;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Flownodes.Worker.Mediator.Handlers;

public class SignInUserHandler : IRequestHandler<SignInUserRequest, SignInUserResponse>
{
    private readonly SignInManager<ApplicationUser> _signInManager;

    private readonly UserManager<ApplicationUser> _userManager;

    public SignInUserHandler(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<SignInUserResponse> Handle(SignInUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByNameAsync(request.Username);
        if (user is null) return new SignInUserResponse("Could not login", ResponseKind.Unauthorized);

        var result = await _signInManager.PasswordSignInAsync(user, request.Password, false, false);
        return result.Succeeded
            ? new SignInUserResponse()
            : new SignInUserResponse("Could not login", ResponseKind.Unauthorized);
    }
}