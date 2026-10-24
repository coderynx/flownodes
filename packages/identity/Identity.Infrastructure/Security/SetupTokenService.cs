using System.Security.Cryptography;
using System.Text;
using Identity.Application.Security;

namespace Identity.Infrastructure.Security;

internal sealed class SetupTokenService : ISetupToken
{
    private readonly object _gate = new();
    private string? _token;

    public string Generate()
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        lock (_gate)
        {
            _token = token;
        }

        return token;
    }

    public bool Matches(string candidate)
    {
        lock (_gate)
        {
            return _token is not null && CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(_token),
                Encoding.UTF8.GetBytes(candidate ?? string.Empty));
        }
    }

    public void Invalidate()
    {
        lock (_gate)
        {
            _token = null;
        }
    }
}
