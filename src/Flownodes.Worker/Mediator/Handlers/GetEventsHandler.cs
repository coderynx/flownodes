using Flownodes.Worker.Mediator.Requests;
using Flownodes.Worker.Mediator.Responses;
using Flownodes.Worker.Services;
using MediatR;

namespace Flownodes.Worker.Mediator.Handlers;

public class GetEventsHandler : IRequestHandler<GetEventsRequest, GetEventsResponse>
{
    public GetEventsHandler(IEnvironmentService environmentService)
    {
        _environmentService = environmentService;
    }

    private readonly IEnvironmentService _environmentService;
    
    public async Task<GetEventsResponse> Handle(GetEventsRequest request, CancellationToken cancellationToken)
    {
        var eventBook = await _environmentService.GetEventBook(request.TenantName);
        if (eventBook is null)
            return new GetEventsResponse(request.TenantName, "Tenant not found", ResponseKind.NotFound);
        
        try
        {
            var events = await eventBook.GetEvents();
            return new GetEventsResponse(request.TenantName, events);
        }
        catch
        {
            return new GetEventsResponse(request.TenantName, "Could not retrieve events",
                ResponseKind.InternalError);
        }

    }
}