using System.Security.Claims;
using Coderynx.MediatorKit.Abstractions;
using Identity.Application.SignIn.FindSignInUser;
using Identity.Presentation.Bootstrap;
using Identity.Presentation.Connect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.Presentation.Pages.Connect;

[AllowAnonymous]
internal sealed class VerifyModel(ISender sender, IOpenIddictApplicationManager applications) : PageModel
{
    [BindProperty(Name = "user_code")] public string? UserCode { get; set; }
    [BindProperty] public string? Decision { get; set; }
    public string? ClientName { get; private set; }
    public string? SignedInAs { get; private set; }
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        UserCode = Request.Query["user_code"].ToString();
        if (string.IsNullOrEmpty(UserCode))
        {
            return Page();
        }

        var session = await HttpContext.AuthenticateAsync(DependencyInjection.SessionScheme);
        if (!session.Succeeded)
        {
            var returnUrl = $"/connect/verify?user_code={Uri.EscapeDataString(UserCode)}";

            return Redirect($"/signin?returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        var pending = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (!pending.Succeeded)
        {
            UserCode = null;
            Error = "That code is not valid or has expired.";

            return Page();
        }

        ClientName = await ClientNameAsync(pending.Principal, cancellationToken);
        SignedInAs = session.Principal.FindFirst("name")?.Value;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var session = await HttpContext.AuthenticateAsync(DependencyInjection.SessionScheme);
        var pending = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (!session.Succeeded || !pending.Succeeded)
        {
            return Challenge(DependencyInjection.SessionScheme);
        }

        if (Decision != "accept")
        {
            return Forbid(
                new AuthenticationProperties(
                    new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.AccessDenied,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The sign-in was denied."
                    }),
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var userId = Guid.Parse(session.Principal.FindFirst("sub")!.Value);
        var found = await sender.SendAsync(new Query(userId), cancellationToken);
        if (found.IsFailure)
        {
            return Challenge(DependencyInjection.SessionScheme);
        }

        var principal = TokenEndpoint.SignInPrincipal(found.Value, pending.Principal.GetScopes());
        foreach (var privateClaim in pending.Principal.Claims.Where(claim =>
                     claim.Type.StartsWith("oi_", StringComparison.Ordinal)))
        {
            ((ClaimsIdentity)principal.Identity!).AddClaim(privateClaim);
        }

        return SignIn(
            principal,
            new AuthenticationProperties
            {
                RedirectUri = "/connect/verified"
            },
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<string?> ClientNameAsync(ClaimsPrincipal pending, CancellationToken ct)
    {
        var clientId = pending.GetClaim(Claims.ClientId);
        var application = clientId is null ? null : await applications.FindByClientIdAsync(clientId, ct);

        return application is null ? clientId : await applications.GetDisplayNameAsync(application, ct);
    }
}
