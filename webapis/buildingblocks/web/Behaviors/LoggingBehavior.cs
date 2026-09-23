using System.Diagnostics;
using Mediator;
using Microsoft.Extensions.Logging;
using yggdrasil.Core.Results;

namespace yggdrasil.Web.Behaviors;

/// <summary>Logs every message handled through the mediator, with its duration and outcome.</summary>
public sealed partial class LoggingBehavior<TMessage, TResponse>(ILogger<LoggingBehavior<TMessage, TResponse>> logger)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : notnull, IMessage
{
    private static readonly string message_name = typeof(TMessage).DeclaringType is { } declaringType
        ? $"{declaringType.Name}.{typeof(TMessage).Name}"
        : typeof(TMessage).Name;

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        var startedAt = Stopwatch.GetTimestamp();
        var response = await next(message, cancellationToken);
        var elapsed = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;

        if (response is Result { IsSuccess: false } result)
            LogFailed(message_name, elapsed, result.Error.Code);
        else
            LogHandled(message_name, elapsed);

        return response;
    }

    [LoggerMessage(LogLevel.Debug, "Handled {MessageName} in {Elapsed:0.0000} ms")]
    private partial void LogHandled(string messageName, double elapsed);

    [LoggerMessage(LogLevel.Information, "Handled {MessageName} in {Elapsed:0.0000} ms with error {ErrorCode}")]
    private partial void LogFailed(string messageName, double elapsed, string errorCode);
}
