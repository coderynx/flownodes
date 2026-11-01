using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;

namespace Web.Session;

internal sealed class SessionStore(IDistributedCache cache, IDataProtector protector) : ITicketStore
{
    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        await RenewAsync(key, ticket);

        return key;
    }

    public Task RenewAsync(string key, AuthenticationTicket ticket) =>
        cache.SetAsync(
            key,
            protector.Protect(TicketSerializer.Default.Serialize(ticket)),
            new DistributedCacheEntryOptions { AbsoluteExpiration = ticket.Properties.ExpiresUtc });

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        var stored = await cache.GetAsync(key);

        return stored is null ? null : TicketSerializer.Default.Deserialize(protector.Unprotect(stored));
    }

    public Task RemoveAsync(string key) => cache.RemoveAsync(key);
}
