namespace Flownodes.Sdk.Resourcing.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class BehaviourIdAttribute : Attribute
{
    public BehaviourIdAttribute(string id)
    {
        Id = id;
    }

    public string Id { get; init; }
}