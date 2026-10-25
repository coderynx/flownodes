namespace Engine.Infrastructure.Integrations;

internal sealed class IntegrationOptions
{
    public const string Section = "Flownodes:Integrations";
    public string? Directory { get; set; }
}
