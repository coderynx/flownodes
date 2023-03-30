namespace Flownodes.Sdk.Extendability;

[AttributeUsage(AttributeTargets.Assembly)]
public class ExtensionDescriptionAttribute : Attribute
{
    public ExtensionDescriptionAttribute(string description)
    {
        Description = description;
    }

    public string Description { get; }
}