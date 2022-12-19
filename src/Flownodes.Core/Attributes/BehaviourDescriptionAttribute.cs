namespace Flownodes.Core.Interfaces;

[AttributeUsage(AttributeTargets.Class)]
public class BehaviourDescriptionAttribute : Attribute
{
    public BehaviourDescriptionAttribute(string description)
    {
        Description = description;
    }

    public string Description { get; init; }
}