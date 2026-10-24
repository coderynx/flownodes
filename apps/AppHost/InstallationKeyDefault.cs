using System.Security.Cryptography;
using Aspire.Hosting.Publishing;

namespace AppHost;

internal sealed class InstallationKeyDefault : ParameterDefault
{
    private const int KeyBytes = 32;
    public override string GetDefaultValue() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(KeyBytes));

    public override void WriteToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteStartObject("generate");
        context.Writer.WriteNumber("minLength", Convert.ToBase64String(new byte[KeyBytes]).Length);
        context.Writer.WriteEndObject();
    }
}
