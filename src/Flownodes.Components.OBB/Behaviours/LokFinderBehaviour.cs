using Flownodes.Components.OBB.ApiSchemas;
using Flownodes.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Refit;

namespace Flownodes.Components.OBB.Behaviours;

public class LokFinderBehaviour : IDataCollectorBehaviour
{
    private readonly ILogger<LokFinderBehaviour> _logger;
    private readonly ILokFinderApi _lokFinderApi;

    public LokFinderBehaviour(ILogger<LokFinderBehaviour> logger)
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