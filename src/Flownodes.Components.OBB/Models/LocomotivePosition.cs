using Newtonsoft.Json;

namespace Flownodes.Components.OBB.Models;

public record LocomotivePosition
{
    [JsonProperty(PropertyName = "train_number")]
    public string? TrainNumber { get; set; }

    [JsonProperty(PropertyName = "unit_number")]
    public string? UnitNumber { get; set; }

    [JsonProperty(PropertyName = "name")] public string? Name { get; set; }

    [JsonProperty(PropertyName = "abbr")] public string? Abbreviation { get; set; }

    [JsonProperty(PropertyName = "lat")] public double? Latitude { get; set; }

    [JsonProperty(PropertyName = "lng")] public double? Longitude { get; set; }

    [JsonProperty(PropertyName = "arrival")]
    public DateTime? ArrivalTime { get; set; }

    [JsonProperty(PropertyName = "departure")]
    public DateTime? DepartureTime { get; set; }
}