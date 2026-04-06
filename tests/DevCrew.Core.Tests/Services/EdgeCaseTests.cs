using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using DevCrew.Core.Application.Services;
using Shouldly;
using Xunit;

namespace DevCrew.Core.Tests.Services;

/// <summary>
/// Edge case and boundary condition tests for critical services
/// </summary>
public class EdgeCaseTests
{
    private readonly JsonFormatterService _jsonFormatterService;
    private readonly Base64EncoderService _base64EncoderService;
    private readonly GuidService _guidService;

    public EdgeCaseTests()
    {
        _jsonFormatterService = new JsonFormatterService();
        _base64EncoderService = new Base64EncoderService();
        _guidService = new GuidService();
    }

    #region JSON Formatter Large Payload Tests

    [Fact]
    public void JsonFormatter_HandleLargePayload_WhenFormattingMegabytesOfJson()
    {
        // Arrange - Create large JSON array (1MB)
        var largeArray = new StringBuilder();
        largeArray.Append("[");
        for (int i = 0; i < 10000; i++)
        {
            if (i > 0) largeArray.Append(",");
            largeArray.Append($"{{\"id\":{i},\"name\":\"item{i}\",\"value\":null}}");
        }
        largeArray.Append("]");

        var largeJson = largeArray.ToString();

        // Act
        var sw = Stopwatch.StartNew();
        var validateResult = _jsonFormatterService.Validate(largeJson);
        sw.Stop();

        // Assert
        validateResult.IsValid.ShouldBeTrue();
        sw.ElapsedMilliseconds.ShouldBeLessThan(5000); // Should complete within 5 seconds
    }

    [Fact]
    public void JsonFormatter_MinifyLargePayload_CompleteWithinReasonableTime()
    {
        // Arrange
        var largeJson = "[" + string.Join(",", Enumerable.Range(0, 5000).Select(i => $"{{\"id\":{i}}}")) + "]";

        // Act
        var sw = Stopwatch.StartNew();
        var minifyResult = _jsonFormatterService.Minify(largeJson);
        sw.Stop();

        // Assert
        minifyResult.IsValid.ShouldBeTrue();
        sw.ElapsedMilliseconds.ShouldBeLessThan(2000);
    }

    [Fact]
    public void JsonFormatter_SortLargePayload_CompleteWithinReasonableTime()
    {
        // Arrange
        var largeJson = "{" + string.Join(",", Enumerable.Range(0, 1000).Select(i => $"\"key{i:D4}\":{i}")) + "}";

        // Act
        var sw = Stopwatch.StartNew();
        var sortResult = _jsonFormatterService.SortKeys(largeJson);
        sw.Stop();

        // Assert
        sortResult.IsValid.ShouldBeTrue();
        sw.ElapsedMilliseconds.ShouldBeLessThan(3000);
    }

    #endregion

    #region Base64 Encoder Large Payload Tests

    [Fact]
    public void Base64Encoder_HandleLargePayload_WhenEncodingMultipleMbOfData()
    {
        // Arrange - 5MB of data
        var largePayload = new byte[5 * 1024 * 1024];
        new Random(42).NextBytes(largePayload);

        // Act
        var sw = Stopwatch.StartNew();
        var encodeResult = _base64EncoderService.Encode(largePayload);
        sw.Stop();

        // Assert
        encodeResult.IsSuccess.ShouldBeTrue();
        sw.ElapsedMilliseconds.ShouldBeLessThan(5000);
    }

    [Fact]
    public void Base64Encoder_RoundtripLargePayload_PreserveExactBytes()
    {
        // Arrange - 2MB of data
        var originalPayload = new byte[2 * 1024 * 1024];
        new Random(123).NextBytes(originalPayload);

        // Act
        var encodeResult = _base64EncoderService.Encode(originalPayload);
        encodeResult.IsSuccess.ShouldBeTrue();

        var decodeResult = _base64EncoderService.Decode(encodeResult.Output);
        decodeResult.IsSuccess.ShouldBeTrue();

        // Assert
        decodeResult.Output!.Length.ShouldBe(originalPayload.Length);
        decodeResult.Output!.ShouldBe(originalPayload);
    }

    [Fact]
    public void Base64Encoder_HandleMaxByteValues_EncodeAllByteRanges()
    {
        // Arrange - Array with all possible byte values
        var allBytes = new byte[256];
        for (int i = 0; i < 256; i++)
        {
            allBytes[i] = (byte)i;
        }

        // Act
        var encodeResult = _base64EncoderService.Encode(allBytes);
        var decodeResult = _base64EncoderService.Decode(encodeResult.Output);

        // Assert
        encodeResult.IsSuccess.ShouldBeTrue();
        decodeResult.IsSuccess.ShouldBeTrue();
        decodeResult.Output.ShouldBe(allBytes);
    }

    #endregion

    #region GUID Service Boundary Tests

    [Fact]
    public void GuidService_GenerateMultipleGuids_AllUnique()
    {
        // Arrange
        var guids = new System.Collections.Generic.HashSet<string>();

        // Act
        for (int i = 0; i < 10000; i++)
        {
            var guid = _guidService.Generate();
            guids.Add(guid);
        }

        // Assert
        guids.Count.ShouldBe(10000); // All unique
    }

    [Fact]
    public void GuidService_GenerateGuids_FormatIsConsistent()
    {
        // Act
        var guid1 = _guidService.Generate();
        var guid2 = _guidService.Generate();
        var guid3 = _guidService.Generate();

        // Assert - All should be valid GUIDs without braces
        guid1.ShouldNotContain("{");
        guid1.ShouldNotContain("}");
        guid2.ShouldNotContain("{");
        guid3.ShouldNotContain("}");

        // All should be parseable
        Guid.TryParse(guid1, out _).ShouldBeTrue();
        Guid.TryParse(guid2, out _).ShouldBeTrue();
        Guid.TryParse(guid3, out _).ShouldBeTrue();
    }

    #endregion

    #region Combined Service Boundary Tests

    [Fact]
    public void MultipleServices_HandleExtremeBoundaries_WithoutExceptions()
    {
        // Arrange
        var extremeJson = "{\"a\":\"" + new string('x', 100000) + "\"}";
        var extremeBytes = new byte[10 * 1024 * 1024]; // 10MB

        // Act & Assert - Should not throw
        var jsonValidate = _jsonFormatterService.Validate(extremeJson);
        jsonValidate.ShouldNotBeNull();

        var base64Encode = _base64EncoderService.Encode(extremeBytes);
        base64Encode.ShouldNotBeNull();

        var guid = _guidService.Generate();
        guid.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void JsonFormatter_HandleDeeplyNestedJson_WithoutStackOverflow()
    {
        // Arrange - Create deeply nested JSON
        var deepJson = new StringBuilder();
        for (int i = 0; i < 1000; i++)
        {
            deepJson.Append("{\"level\":");
        }
        deepJson.Append("\"end\"");
        for (int i = 0; i < 1000; i++)
        {
            deepJson.Append("}");
        }

        var json = deepJson.ToString();

        // Act
        var validateResult = _jsonFormatterService.Validate(json);

        // Assert
        validateResult.ShouldNotBeNull();
        // May succeed or fail gracefully depending on depth limit, but should not crash
    }

    #endregion
}
