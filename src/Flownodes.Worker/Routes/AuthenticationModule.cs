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
            async (UserManager<ApplicationUser> userManager, User user) =>
            {
                var applicationUser = new ApplicationUser
                {
                    UserName = user.Username,
                    Email = user.Email
                };
                var result = await userManager.CreateAsync(applicationUser, user.Password);

                return result.Succeeded ? Results.Ok() : Results.BadRequest(result.Errors);
            });

        app.MapPost("/api/authentication/login", [AllowAnonymous]
            async (UserManager<ApplicationUser> userManager, User user) =>
            {
                var applicationUser = await userManager.FindByNameAsync(user.Username);

                if (applicationUser is not null && await userManager.CheckPasswordAsync(applicationUser, user.Password))
                    return Results.Ok("Authorized");

                return Results.Unauthorized();
            });

        app.MapPost("/api/authentication/apikey", [AllowAnonymous]
            async (IMediator mediator, CreateApiKeyRequest request) =>
            {
                var response = await mediator.Send(request);
                return response.GetResult();
            });
    }
}