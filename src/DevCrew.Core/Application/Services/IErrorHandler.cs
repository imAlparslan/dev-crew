using Microsoft.Extensions.Logging;

namespace DevCrew.Core.Application.Services;

/// <summary>
/// Interface for standardized error handling across the application.
/// Provides consistent logging and error reporting mechanisms.
/// </summary>
public interface IErrorHandler
{
    /// <summary>
    /// Logs an exception with optional context message.
    /// </summary>
    /// <param name="exception">The exception to log</param>
    /// <param name="message">Optional context message</param>
    void LogException(Exception exception, string? message = null);

    /// <summary>
    /// Executes an action and handles any exceptions that occur.
    /// </summary>
    /// <param name="action">The action to execute</param>
    /// <param name="operationName">Name of the operation for logging</param>
    /// <returns>True if successful, false if exception occurred</returns>
    bool TryExecute(Action action, string? operationName = null);

    /// <summary>
    /// Executes an async action and handles any exceptions that occur.
    /// </summary>
    /// <param name="action">The async action to execute</param>
    /// <param name="operationName">Name of the operation for logging</param>
    /// <returns>True if successful, false if exception occurred</returns>
    Task<bool> TryExecuteAsync(Func<Task> action, string? operationName = null);

    /// <summary>
    /// Executes a function and handles any exceptions that occur.
    /// </summary>
    /// <param name="func">The function to execute</param>
    /// <param name="operationName">Name of the operation for logging</param>
    /// <returns>A tuple of (success, result). Result is default if exception occurred.</returns>
    (bool Success, T? Result) TryExecute<T>(Func<T> func, string? operationName = null);

    /// <summary>
    /// Executes an async function and handles any exceptions that occur.
    /// </summary>
    /// <param name="func">The async function to execute</param>
    /// <param name="operationName">Name of the operation for logging</param>
    /// <returns>A tuple of (success, result). Result is default if exception occurred.</returns>
    Task<(bool Success, T? Result)> TryExecuteAsync<T>(Func<Task<T>> func, string? operationName = null);
}

/// <summary>
/// Standard implementation of error handling with structured logging.
/// Uses ILogger for production-ready error logging.
/// </summary>
public class ErrorHandler : IErrorHandler
{
    private readonly ILogger<ErrorHandler> _logger;

    public ErrorHandler(ILogger<ErrorHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc/>
    public void LogException(Exception? exception, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var errorMessage = string.IsNullOrWhiteSpace(message)
            ? exception.Message
            : $"{message} - {exception.Message}";

        _logger.LogError(exception, errorMessage);
    }

    /// <inheritdoc/>
    public bool TryExecute(Action action, string? operationName = null)
    {
        try
        {
            action();
            return true;
        }
        catch (Exception ex)
        {
            LogException(ex, operationName ?? "Operation");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> TryExecuteAsync(Func<Task> action, string? operationName = null)
    {
        try
        {
            await action();
            return true;
        }
        catch (Exception ex)
        {
            LogException(ex, operationName ?? "Async operation");
            return false;
        }
    }

    /// <inheritdoc/>
    public (bool Success, T? Result) TryExecute<T>(Func<T> func, string? operationName = null)
    {
        try
        {
            var result = func();
            return (true, result);
        }
        catch (Exception ex)
        {
            LogException(ex, operationName ?? "Operation");
            return (false, default);
        }
    }

    /// <inheritdoc/>
    public async Task<(bool Success, T? Result)> TryExecuteAsync<T>(Func<Task<T>> func, string? operationName = null)
    {
        try
        {
            var result = await func();
            return (true, result);
        }
        catch (Exception ex)
        {
            LogException(ex, operationName ?? "Async operation");
            return (false, default);
        }
    }
}
