using Carter;
using Flownodes.Shared.Authentication.Models;
using Flownodes.Worker.Extensions;
using Flownodes.Worker.Mediator.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Flownodes.Worker.Routes;

public record User(string Username, string Email, string Password);

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

        app.MapPost("/api/authentication/login", [AllowAnonymous]
            async (SignInManager<ApplicationUser> manager, User request) =>
            {
                var user = new ApplicationUser
                {
                    UserName = request.Username,
                    Email = request.Email
                };
                var result = await manager.CheckPasswordSignInAsync(user, request.Password, false);
                return result.Succeeded ? Results.Ok() : Results.Unauthorized();
            });

        app.MapPost("/api/authentication/apikey", [AllowAnonymous]
            async (IMediator mediator, CreateApiKeyRequest request) =>
            {
                var response = await mediator.Send(request);
                return response.GetResult();
            });
    }
}