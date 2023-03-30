namespace Flownodes.Sdk.Extendability;

[AttributeUsage(AttributeTargets.Assembly)]
public class ExtensionAuthorAttribute : Attribute
{
    public ExtensionAuthorAttribute(string author)
    {
        Author = author;
    }

    public string Author { get; }
}