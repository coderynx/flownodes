using Coderynx.Functional.Results;
using Coderynx.Functional.Results.Errors;
using Coderynx.MediatorKit.Abstractions;
using Microsoft.Extensions.Logging;

namespace Core.Application.Behaviors;

internal sealed partial class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IRequestPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken = default)
    {
        var requestName = typeof(TRequest).Name;

        LogStarted(requestName);

        TResponse response;
        try
        {
            response = await next();
        }
        catch (Exception exception)
        {
            LogThrew(exception, requestName);
            throw;
        }

        if (response is Result { IsFailure: true } result)
        {
            LogFailed(requestName, result.Error.Kind, result.Error.Code, result.Error.Message);
        }
        else
        {
            LogCompleted(requestName);
        }

        return response;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Handling {RequestName}")]
    private partial void LogStarted(string requestName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Handled {RequestName}")]
    private partial void LogCompleted(string requestName);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Handled {RequestName} with failure {ErrorKind} {ErrorCode}: {ErrorMessage}")]
    private partial void LogFailed(string requestName, ErrorKind errorKind, string errorCode, string errorMessage);

    [LoggerMessage(Level = LogLevel.Error, Message = "Handler for {RequestName} threw")]
    private partial void LogThrew(Exception exception, string requestName);
}
