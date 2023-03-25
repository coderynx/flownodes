using Carter;
using Flownodes.Shared.Authentication.Models;
using Flownodes.Worker.Extensions;
using Flownodes.Worker.Mediator.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Flownodes.Worker.Routes;

public record User(string Username, string Password);

public class AuthenticationModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/authentication/users", [AllowAnonymous]
            async (IMediator mediator, CreateUserRequest request) =>
            {
                var response = await mediator.Send(request);
                return response.GetResult();
            });

        app.MapPost("/api/authentication/signin", [AllowAnonymous]
            async (SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager,
                User request) =>
            {
                var user = await userManager.FindByNameAsync(request.Username);
                if (user is null) return Results.Unauthorized();
                var result = await signInManager.PasswordSignInAsync(user, request.Password, false, false);
                return result.Succeeded ? Results.Ok() : Results.Unauthorized();
            });

        app.MapPost("/api/authentication/signout",
            [AllowAnonymous] async (SignInManager<ApplicationUser> signInManager) =>
            {
                await signInManager.SignOutAsync();
            });

        app.MapPost("/api/authentication/apikey", [AllowAnonymous]
            async (IMediator mediator, CreateApiKeyRequest request) =>
            {
                var response = await mediator.Send(request);
                return response.GetResult();
            });
    }
}