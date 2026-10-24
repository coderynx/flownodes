namespace Identity.Infrastructure.Database;

internal sealed class SigningKey
{
    public const string CurrentId = "current";
    public required string Id { get; init; }
    public required byte[] EncryptedPrivateKey { get; init; }
}
