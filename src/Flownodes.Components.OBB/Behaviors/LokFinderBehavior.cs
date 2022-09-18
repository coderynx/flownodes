using Flownodes.Edge.Core.Resources;
using Flownodes.Components.OBB.ApiSchemas;
using Microsoft.Extensions.Logging;
using Refit;

namespace Flownodes.Components.OBB.Behaviors;

public class LokFinderBehavior : IDataCollectorBehavior
{
    private readonly ILogger<LokFinderBehavior> _logger;
    private readonly ILokFinderApi _lokFinderApi;

    public LokFinderBehavior(ILogger<LokFinderBehavior> logger)
    {
        _logger = logger;
        _lokFinderApi = RestService.For<ILokFinderApi>("https://konzern-apps.web.oebb.at/", new RefitSettings
        {
            ContentSerializer = new NewtonsoftJsonContentSerializer()
        });
    }

    public async Task<object?> UpdateAsync(string actionId, Dictionary<string, object?> parameters)
    {
        switch (actionId)
        {
            case "get-loco":

                var response = await _lokFinderApi.GetLocoPositions();
                if (response.IsSuccessStatusCode)
                {
                    var unitNumber = parameters["unit_number"]?.ToString();
                    var loco = response.Content.FirstOrDefault(x => x.UnitNumber == unitNumber);
                    return loco;
                }

                break;
        }

        return null;
    }
}