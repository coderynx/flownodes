namespace Flownodes.Sdk.Extendability;

[AttributeUsage(AttributeTargets.Assembly)]
public sealed class ExtensionIdAttribute : Attribute
{
    public ExtensionIdAttribute(string id)
    {
        Id = id;
    }

    public string Id { get; }
}