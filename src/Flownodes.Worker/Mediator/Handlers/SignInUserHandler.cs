using Flownodes.Shared.Authentication.Models;
using Flownodes.Worker.Mediator.Requests;
using Flownodes.Worker.Mediator.Responses;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Flownodes.Worker.Mediator.Handlers;

public class SignInUserHandler : IRequestHandler<SignInUserRequest, SignInUserResponse>
{
    public SignInUserHandler(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public async Task<SignInUserResponse> Handle(SignInUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByNameAsync(request.Username);
        if(user is null) return new SignInUserResponse("Could not login", ResponseKind.BadRequest);

        var result = await _signInManager.PasswordSignInAsync(user, request.Password, false, false);
        return new SignInUserResponse(result);
    }
}