using System;
using System.Linq;
using System.Threading.Tasks;
using DevCrew.Core.Application.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace DevCrew.Core.Tests.Services;

public sealed class ErrorHandlerTests
{
    private readonly ILogger<ErrorHandler> _mockLogger;
    private readonly ErrorHandler _errorHandler;

    public ErrorHandlerTests()
    {
        _mockLogger = Substitute.For<ILogger<ErrorHandler>>();
        _errorHandler = new ErrorHandler(_mockLogger);
    }

    #region LogException Tests

    [Fact]
    public void LogException_CatchAndLog_WhenExceptionOccurs()
    {
        // Arrange
        var exception = new ArgumentException("Test error");
        var message = "Test context";

        // Act
        _errorHandler.LogException(exception, message);

        // Assert - LogError should have been called
        var callsReceived = _mockLogger.ReceivedCalls().Count();
        callsReceived.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void LogException_ThrowArgumentNull_WhenExceptionIsNull()
    {
        // Arrange
        Exception exceptionNull = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _errorHandler.LogException(exceptionNull));
    }

    #endregion

    #region TryExecute Sync Tests

    [Fact]
    public void TryExecute_ReturnTrue_WhenActionSucceeds()
    {
        // Arrange
        var executed = false;
        Action action = () => executed = true;

        // Act
        var result = _errorHandler.TryExecute(action);

        // Assert
        result.ShouldBeTrue();
        executed.ShouldBeTrue();
    }

    [Fact]
    public void TryExecute_ReturnFalseOnError_WhenActionThrows()
    {
        // Arrange
        Action action = () => throw new InvalidOperationException("Test error");

        // Act
        var result = _errorHandler.TryExecute(action, "TestOperation");

        // Assert
        result.ShouldBeFalse();
        var callsReceived = _mockLogger.ReceivedCalls().Count();
        callsReceived.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void TryExecute_LogWithOperationName_WhenOperationNameProvided()
    {
        // Arrange
        Action action = () => throw new InvalidOperationException("Test error");
        var operationName = "MyOperation";

        // Act
        var result = _errorHandler.TryExecute(action, operationName);

        // Assert
        result.ShouldBeFalse();
        // Verify logging was called
        var callsReceived = _mockLogger.ReceivedCalls().Count();
        callsReceived.ShouldBeGreaterThan(0);
    }

    #endregion

    #region TryExecuteAsync Tests

    [Fact]
    public async Task TryExecuteAsync_ReturnTrue_WhenActionSucceeds()
    {
        // Arrange
        var executed = false;
        Func<Task> action = async () =>
        {
            executed = true;
            await Task.CompletedTask;
        };

        // Act
        var result = await _errorHandler.TryExecuteAsync(action);

        // Assert
        result.ShouldBeTrue();
        executed.ShouldBeTrue();
    }

    [Fact]
    public async Task TryExecuteAsync_ReturnFalseOnError_WhenTaskThrows()
    {
        // Arrange
        Func<Task> action = async () =>
        {
            await Task.CompletedTask;
            throw new InvalidOperationException("Async error");
        };

        // Act
        var result = await _errorHandler.TryExecuteAsync(action, "AsyncOperation");

        // Assert
        result.ShouldBeFalse();
        var callsReceived = _mockLogger.ReceivedCalls().Count();
        callsReceived.ShouldBeGreaterThan(0);
    }

    #endregion

    #region TryExecute Generic Tests

    [Fact]
    public void TryExecuteGeneric_ReturnSuccess_WhenFuncReturnsValue()
    {
        // Arrange
        Func<int> func = () => 42;

        // Act
        var (success, result) = _errorHandler.TryExecute(func);

        // Assert
        success.ShouldBeTrue();
        result.ShouldBe(42);
    }

    [Fact]
    public void TryExecuteGeneric_ReturnFalseAndDefault_WhenFuncThrows()
    {
        // Arrange
        Func<string> func = () => throw new InvalidOperationException("Error");

        // Act
        var (success, result) = _errorHandler.TryExecute(func, "GetValue");

        // Assert
        success.ShouldBeFalse();
        result.ShouldBeNull();
        var callsReceived = _mockLogger.ReceivedCalls().Count();
        callsReceived.ShouldBeGreaterThan(0);
    }

    #endregion

    #region TryExecuteAsync Generic Tests

    [Fact]
    public async Task TryExecuteAsyncGeneric_ReturnSuccess_WhenFuncReturnsValue()
    {
        // Arrange
        Func<Task<string>> func = async () =>
        {
            await Task.CompletedTask;
            return "result value";
        };

        // Act
        var (success, result) = await _errorHandler.TryExecuteAsync(func);

        // Assert
        success.ShouldBeTrue();
        result.ShouldBe("result value");
    }

    [Fact]
    public async Task TryExecuteAsyncGeneric_ReturnFalseAndDefault_WhenFuncThrows()
    {
        // Arrange
        Func<Task<int>> func = async () =>
        {
            await Task.CompletedTask;
            throw new InvalidOperationException("Async error");
        };

        // Act
        var (success, result) = await _errorHandler.TryExecuteAsync(func, "AsyncValue");

        // Assert
        success.ShouldBeFalse();
        result.ShouldBe(default(int));
        var callsReceived = _mockLogger.ReceivedCalls().Count();
        callsReceived.ShouldBeGreaterThan(0);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void TryExecute_HandleMultipleFailures_IndependentLogging()
    {
        // Arrange
        Action action1 = () => throw new InvalidOperationException("Error 1");
        Action action2 = () => throw new ArgumentException("Error 2");

        // Act
        var result1 = _errorHandler.TryExecute(action1, "Operation1");
        var result2 = _errorHandler.TryExecute(action2, "Operation2");

        // Assert
        result1.ShouldBeFalse();
        result2.ShouldBeFalse();
        // Should have at least 2 logging calls (one for each error)
        var callsReceived = _mockLogger.ReceivedCalls().Count();
        callsReceived.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task MixedSyncAndAsync_WorkIndependently()
    {
        // Arrange
        var syncExecuted = false;
        var asyncExecuted = false;
        Action syncAction = () => syncExecuted = true;
        Func<Task> asyncAction = async () =>
        {
            asyncExecuted = true;
            await Task.CompletedTask;
        };

        // Act
        var syncResult = _errorHandler.TryExecute(syncAction);
        var asyncResult = await _errorHandler.TryExecuteAsync(asyncAction);

        // Assert
        syncResult.ShouldBeTrue();
        asyncResult.ShouldBeTrue();
        syncExecuted.ShouldBeTrue();
        asyncExecuted.ShouldBeTrue();
    }

    #endregion
}

