namespace Identity.Application;

/// <summary>
/// The token types Identity's token endpoint understands in a token exchange request.
/// </summary>
public static class ExchangeTokenTypes
{
    /// <summary>
    /// The <c>subject_token_type</c> of a Personal Access Token exchanged for an access token.
    /// </summary>
    public const string PersonalAccessToken = "urn:flownodes:params:oauth:token-type:personal-access-token";
}
