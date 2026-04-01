using CommunityToolkit.Mvvm.ComponentModel;
using DevCrew.Core.Application.Services;

namespace DevCrew.Desktop.ViewModels;

/// <summary>
/// Base ViewModel class providing common functionality for all ViewModels.
/// </summary>
public abstract class BaseViewModel : ObservableObject
{
    protected readonly IErrorHandler ErrorHandler;
    
    private bool _isLoading;
    private string? _errorMessage;

    /// <summary>
    /// Initializes a new instance of the BaseViewModel class.
    /// </summary>
    /// <param name="errorHandler">The error handler for centralized error logging</param>
    protected BaseViewModel(IErrorHandler errorHandler)
    {
        ErrorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
    }

    /// <summary>
    /// Loading state
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    /// <summary>
    /// Error message
    /// </summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    /// <summary>
    /// Clears the error state.
    /// </summary>
    public void ClearError()
    {
        ErrorMessage = null;
    }

    /// <summary>
    /// Executes an async operation with automatic loading state and error handling.
    /// </summary>
    /// <param name="operation">The async operation to execute</param>
    /// <param name="errorContext">Optional error context message if operation fails</param>
    /// <returns>True if operation succeeded, false otherwise</returns>
    protected async Task<bool> RunAsyncOperationAsync(Func<Task> operation, string? errorContext = null)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        try
        {
            ClearError();
            IsLoading = true;
            await operation();
            return true;
        }
        catch (Exception ex)
        {
            ErrorHandler.LogException(ex, errorContext);
            ErrorMessage = string.IsNullOrWhiteSpace(errorContext)
                ? ex.Message
                : $"{errorContext}: {ex.Message}";
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Executes an async operation that returns a result with automatic loading state and error handling.
    /// </summary>
    /// <param name="operation">The async operation to execute</param>
    /// <param name="errorContext">Optional error context message if operation fails</param>
    /// <returns>A tuple of (success, result). Result is default if operation failed.</returns>
    protected async Task<(bool Success, T? Result)> RunAsyncOperationAsync<T>(Func<Task<T>> operation, string? errorContext = null)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        try
        {
            ClearError();
            IsLoading = true;
            var result = await operation();
            return (true, result);
        }
        catch (Exception ex)
        {
            ErrorHandler.LogException(ex, errorContext);
            ErrorMessage = string.IsNullOrWhiteSpace(errorContext)
                ? ex.Message
                : $"{errorContext}: {ex.Message}";
            return (false, default);
        }
        finally
        {
            IsLoading = false;
        }
    }
}