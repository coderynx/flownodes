namespace Identity.Application.Security;

/// <summary>
/// The one-time token a Silo logs while the installation has no User, which proves to the sign-in page that its holder
/// can read the Silo's log (ADR-0026).
/// </summary>
public interface ISetupToken
{
    /// <summary>
    /// Determines whether a candidate is the token this Silo generated and has not yet been used.
    /// </summary>
    /// <param name="candidate">The text the person entered.</param>
    /// <returns><see langword="true"/> when the candidate is the live token.</returns>
    bool Matches(string candidate);

    /// <summary>
    /// Makes the token worthless; it is called once the first User exists.
    /// </summary>
    void Invalidate();
}
