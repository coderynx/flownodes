using Microsoft.EntityFrameworkCore;
using Registry.Application.Database;
using Registry.Application.Resources;
using Registry.Domain.Resources;

namespace Registry.Application.Secrets;

internal static class SecretStorage
{
    public static async Task StoreAsync(
        this IApplicationDbContext database,
        ISecretVault vault,
        ManifestEntry entry,
        CancellationToken cancellationToken)
    {
        var held = await HeldAsync(database, entry, cancellationToken);
        var values = entry.Values!;

        database.SecretValues.RemoveRange(held.Where(value => !values.ContainsKey(value.Key)));
        foreach (var (key, value) in values)
        {
            var encrypted = vault.Encrypt(value);
            if (held.Find(existing => existing.Key == key) is { } current)
            {
                current.Replace(encrypted);
            }
            else
            {
                database.SecretValues.Add(SecretValue.Create(entry.Namespace, entry.Name, key, encrypted));
            }
        }
    }

    public static async Task ForgetAsync(
        this IApplicationDbContext database,
        ManifestEntry entry,
        CancellationToken cancellationToken) =>
        database.SecretValues.RemoveRange(await HeldAsync(database, entry, cancellationToken));

    private static Task<List<SecretValue>> HeldAsync(
        IApplicationDbContext database,
        ManifestEntry entry,
        CancellationToken cancellationToken) =>
        database.SecretValues.Where(value => value.Namespace == entry.Namespace && value.Secret == entry.Name)
            .ToListAsync(cancellationToken);
}
