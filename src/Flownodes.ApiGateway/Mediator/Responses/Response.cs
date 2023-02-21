namespace Flownodes.ApiGateway.Mediator.Responses;

public abstract record Response
{
    protected Response(string message)
    {
        Message = message;
        IsSuccess = false;
    }

    protected Response()
    {
        IsSuccess = true;
    }

    public bool IsSuccess { get; }
    public string? Message { get; }
}