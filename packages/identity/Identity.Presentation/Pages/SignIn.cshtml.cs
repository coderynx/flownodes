using System.Security.Claims;
using Coderynx.MediatorKit.Abstractions;
using Identity.Domain.Users;
using Identity.Presentation.Bootstrap;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Identity.Presentation.Pages;

[AllowAnonymous]
internal sealed class SignInModel(ISender sender) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
    [BindProperty] public string Username { get; set; } = string.Empty;
    [BindProperty] public string Password { get; set; } = string.Empty;
    [BindProperty] public string SetupToken { get; set; } = string.Empty;
    [BindProperty] public string? DisplayName { get; set; }
    public bool SetupRequired { get; private set; }
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        SetupRequired = await IsSetupRequiredAsync(cancellationToken);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var result = await sender.SendAsync(
            new Application.SignIn.VerifyCredentials.Command(Username, Password),
            cancellationToken);
        if (result.IsFailure)
        {
            Error = result.Error.Message;
            Password = string.Empty;

            return Page();
        }

        return await SignInAsync(result.Value);
    }

    public async Task<IActionResult> OnPostSetupAsync(CancellationToken cancellationToken)
    {
        var result = await sender.SendAsync(
            new Application.Setup.CompleteSetup.Command(SetupToken, Username, DisplayName, Password),
            cancellationToken);
        if (result.IsFailure && result.Error.Kind == Coderynx.Functional.Results.Errors.ErrorKind.NotFound)
        {
            return NotFound();
        }

        if (result.IsFailure)
        {
            SetupRequired = true;
            Error = result.Error.Message;
            Password = string.Empty;

            return Page();
        }

        return await SignInAsync(result.Value);
    }

    private async Task<bool> IsSetupRequiredAsync(CancellationToken cancellationToken)
    {
        var state = await sender.SendAsync(new Application.Setup.GetSetupState.Query(), cancellationToken);

        return state.IsSuccess && state.Value;
    }

    private async Task<IActionResult> SignInAsync(User user)
    {
        var identity = new ClaimsIdentity(DependencyInjection.SessionScheme);
        identity.AddClaim(new Claim("sub", user.Id.ToString()));
        identity.AddClaim(new Claim("name", user.Username.Value));

        await HttpContext.SignInAsync(DependencyInjection.SessionScheme, new ClaimsPrincipal(identity));

        return Url.IsLocalUrl(ReturnUrl) ? LocalRedirect(ReturnUrl) : LocalRedirect("/");
    }
}
