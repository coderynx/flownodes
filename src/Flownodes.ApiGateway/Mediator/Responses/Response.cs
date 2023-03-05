using System.Text.Json.Serialization;

namespace Flownodes.ApiGateway.Mediator.Responses;

public enum ResponseKind
{
    Ok,
    NotFound,
    InternalError,
    BadRequest
}

public abstract record Response
{
    protected Response(string message, ResponseKind responseKind)
    {
        Message = message;
        IsSuccess = false;
        ResponseKind = responseKind;
    }

    protected Response()
    {
        IsSuccess = true;
    }

    public bool IsSuccess { get; }
    public string? Message { get; }

    [JsonIgnore] public ResponseKind ResponseKind { get; }
}