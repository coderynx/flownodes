using Carter;
using Flownodes.Worker.Extensions;
using Flownodes.Worker.Mediator.Requests;
using MediatR;

namespace Flownodes.Worker.Routes;

public class EventsModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/tenants/{tenantName}/events", async (IMediator mediator, string tenantName) =>
            {
                var request = new GetEventsRequest(tenantName);
                var response = await mediator.Send(request);

                return response.GetResult();
            })
            .WithName("GetEvents")
            .WithDisplayName("Get events");
    }
}