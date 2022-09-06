using Flownodes.Components.OBB.Models;
using Refit;

namespace Flownodes.Components.OBB.ApiSchemas;

public interface ILokFinderApi
{
    [Get("/lok/index")]
    Task<ApiResponse<List<LocomotivePosition>>> GetLocoPositions();
}