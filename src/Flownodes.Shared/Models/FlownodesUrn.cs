using System.Text.RegularExpressions;

namespace Flownodes.Shared.Models;

public partial class FlownodesUrn
{
    public FlownodesUrn(string urn)
    {
        _urn = urn;
        
        if (!Validate())
        {
            throw new ArgumentException("Invalid URN");
        }
    }
    
    public FlownodesUrn(string serviceId, string clusterId, string tenantId, string resourceId)
    {
        _urn = $"urn:flownodes:{serviceId}:{clusterId}:{tenantId}:{resourceId}";
        
        if (!Validate())
        {
            throw new ArgumentException("Invalid URN");
        }
    }

    private readonly string _urn;

    public string ServiceId => _urn.Split(':')[2];
    public string ClusterId => _urn.Split(':')[3];
    public string TenantId => _urn.Split(':')[4];
    public string ResourceId => _urn.Split(':')[5];
    
    private bool Validate()
    {
        var regex = UrnRegex();
        return _urn.Length != 0 && regex.IsMatch(_urn);
    }
    
    public override string ToString()
    {
        return _urn;
    }

    public static explicit operator string(FlownodesUrn urn)
    {
        return urn.ToString();
    }
    
    public static explicit operator FlownodesUrn(string urn)
    {
        return new FlownodesUrn(urn);
    }
    
    [GeneratedRegex("urn:flownodes:[a-zA-Z0-9]+:[a-zA-Z0-9]+:[a-zA-Z0-9]+:[a-zA-Z0-9]+$")]
    private static partial Regex UrnRegex();
}