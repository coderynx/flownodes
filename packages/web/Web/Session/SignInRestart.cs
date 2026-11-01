using Microsoft.AspNetCore.Authentication;

namespace Web.Session;

internal static class SignInRestart
{
    private const string RestartedItem = "web.restarted";

    public static async Task RestartOnceAsync(RemoteFailureContext context)
    {
        if (context.Properties?.Items.ContainsKey(RestartedItem) == true)
        {
            return;
        }

        var properties = new AuthenticationProperties { RedirectUri = context.Properties?.RedirectUri ?? "/" };
        properties.Items[RestartedItem] = bool.TrueString;

        await context.HttpContext.ChallengeAsync(SessionNames.SignInScheme, properties);
        context.HandleResponse();
    }
}
