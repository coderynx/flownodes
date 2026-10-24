namespace Identity.Application;

/// <summary>
/// The codes of the errors that stand for an HTTP authentication outcome rather than a problem with the input.
/// </summary>
public static class ErrorCodes
{
    /// <summary>
    /// The caller is not an enabled User of this installation: the response is 401.
    /// </summary>
    public const string Unauthenticated = "identity.unauthenticated";

    /// <summary>
    /// The caller is a User who may not do this: the response is 403.
    /// </summary>
    public const string Forbidden = "identity.forbidden";
}
