namespace Flownodes.Sdk.Alerting;

[AttributeUsage(AttributeTargets.Class)]
public class AlerterDriverIdAttribute : Attribute
{
    public AlerterDriverIdAttribute(string id)
    {
        Id = id;
    }

    public string Id { get; set; }
}

public interface IAlerterDriver
{
    Task SendAlertAsync(AlertToFire alert);
}