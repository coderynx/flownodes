namespace Flownodes.Sdk;

/// <summary>
/// Marks an assembly as the entry assembly of an Integration and names the type that describes it.
/// </summary>
/// <param name="entryType">
/// A type with a public parameterless constructor that implements <see cref="IIntegration"/>.
/// </param>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class FlownodesIntegrationAttribute(Type entryType) : Attribute
{
    /// <summary>
    /// Gets the type that describes the Integration.
    /// </summary>
    public Type EntryType { get; } = entryType;
}
