using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DevCrew.Core.Application.Services;
using Shouldly;
using Xunit;

namespace DevCrew.Core.Tests.Services;

public sealed class Base64EncoderServiceTests
{
    private readonly Base64EncoderService _service = new();

    #region Encode Tests

    [Fact]
    public void Encode_ReturnBase64String_WhenInputBytesAreValid()
    {
        // Arrange
        var inputBytes = Encoding.UTF8.GetBytes("Hello World");

        // Act
        var result = _service.Encode(inputBytes);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Output.ShouldBe("SGVsbG8gV29ybGQ=");
    }

    [Fact]
    public void Encode_ReturnFalse_WhenInputBytesAreNull()
    {
        // Arrange
        byte[] nullBytes = null;

        // Act
        var result = _service.Encode(nullBytes);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorKey.ShouldBe("base64.encode_input_required");
    }

    [Fact]
    public void Encode_ReturnFalse_WhenInputBytesAreEmpty()
    {
        // Arrange
        var emptyBytes = Array.Empty<byte>();

        // Act
        var result = _service.Encode(emptyBytes);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorKey.ShouldBe("base64.encode_input_required");
    }

    [Fact]
    public void Encode_ReturnCorrectFormat_WhenInputContainsUnicode()
    {
        // Arrange
        var unicodeBytes = Encoding.UTF8.GetBytes("Hello 👋 Unicode");

        // Act
        var result = _service.Encode(unicodeBytes);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Output.ShouldNotBeNullOrWhiteSpace();
        // Verify it can be decoded back to unicode
        var decodedBytes = Convert.FromBase64String(result.Output);
        var decodedText = Encoding.UTF8.GetString(decodedBytes);
        decodedText.ShouldBe("Hello 👋 Unicode");
    }

    [Fact]
    public void Encode_ReturnBase64_WhenInputContainsSpecialCharacters()
    {
        // Arrange
        var specialBytes = Encoding.UTF8.GetBytes("!@#$%^&*()_+-=[]{}|;:',.<>?/");

        // Act
        var result = _service.Encode(specialBytes);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Output.ShouldNotBeNullOrWhiteSpace();
    }

    #endregion

    #region Decode Tests

    [Fact]
    public void Decode_ReturnOriginalBytes_WhenBase64IsValid()
    {
        // Arrange
        var base64String = "SGVsbG8gV29ybGQ=";
        var expectedBytes = Encoding.UTF8.GetBytes("Hello World");

        // Act
        var result = _service.Decode(base64String);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Output.ShouldBe(expectedBytes);
    }

    [Fact]
    public void Decode_ReturnFalse_WhenBase64IsInvalid()
    {
        // Arrange
        var invalidBase64 = "!!!@@@###";

        // Act
        var result = _service.Decode(invalidBase64);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorKey.ShouldBe("base64.decode_invalid_format");
    }

    [Fact]
    public void Decode_ReturnFalse_WhenInputIsNull()
    {
        // Arrange
        string nullInput = null;

        // Act
        var result = _service.Decode(nullInput);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorKey.ShouldBe("base64.decode_input_required");
    }

    [Fact]
    public void Decode_ReturnFalse_WhenInputIsEmpty()
    {
        // Arrange
        var emptyInput = string.Empty;

        // Act
        var result = _service.Decode(emptyInput);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorKey.ShouldBe("base64.decode_input_required");
    }

    [Fact]
    public void Decode_ReturnCorrectUnicode_WhenBase64ContainsUnicodeBytes()
    {
        // Arrange
        var unicodeText = "Hello 👋 Unicode";
        var unicodeBytes = Encoding.UTF8.GetBytes(unicodeText);
        var base64String = Convert.ToBase64String(unicodeBytes);

        // Act
        var result = _service.Decode(base64String);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var decodedText = Encoding.UTF8.GetString(result.Output);
        decodedText.ShouldBe(unicodeText);
    }

    [Fact]
    public void Decode_TrimWhitespace_WhenInputHasLeadingOrTrailingWhitespace()
    {
        // Arrange
        var base64String = "  SGVsbG8gV29ybGQ=  ";

        // Act
        var result = _service.Decode(base64String);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var decodedText = Encoding.UTF8.GetString(result.Output);
        decodedText.ShouldBe("Hello World");
    }

    #endregion

    #region Roundtrip Tests

    [Fact]
    public void EncodeDecodeRoundTrip_PreserveOriginalData_WhenUsingSimpleText()
    {
        // Arrange
        var originalText = "This is a test message";
        var originalBytes = Encoding.UTF8.GetBytes(originalText);

        // Act
        var encodeResult = _service.Encode(originalBytes);
        var decodeResult = _service.Decode(encodeResult.Output);

        // Assert
        encodeResult.IsSuccess.ShouldBeTrue();
        decodeResult.IsSuccess.ShouldBeTrue();
        decodeResult.Output.ShouldBe(originalBytes);
        var decodedText = Encoding.UTF8.GetString(decodeResult.Output);
        decodedText.ShouldBe(originalText);
    }

    [Fact]
    public void EncodeDecodeRoundTrip_PreserveOriginalData_WhenUsingUnicodeText()
    {
        // Arrange
        var originalText = "Unicode: 你好 🌍 مرحبا";
        var originalBytes = Encoding.UTF8.GetBytes(originalText);

        // Act
        var encodeResult = _service.Encode(originalBytes);
        var decodeResult = _service.Decode(encodeResult.Output);

        // Assert
        encodeResult.IsSuccess.ShouldBeTrue();
        decodeResult.IsSuccess.ShouldBeTrue();
        var decodedText = Encoding.UTF8.GetString(decodeResult.Output);
        decodedText.ShouldBe(originalText);
    }

    [Fact]
    public void EncodeDecodeRoundTrip_PreserveLargePayload_WhenUsingMegabytesOfData()
    {
        // Arrange
        var largeBytes = new byte[1_000_000]; // 1MB
        var random = new Random(42);
        random.NextBytes(largeBytes);

        // Act
        var encodeResult = _service.Encode(largeBytes);
        var decodeResult = _service.Decode(encodeResult.Output);

        // Assert
        encodeResult.IsSuccess.ShouldBeTrue();
        decodeResult.IsSuccess.ShouldBeTrue();
        decodeResult.Output.ShouldBe(largeBytes);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Encode_ReturnBase64_WhenInputContainsAllByteValues()
    {
        // Arrange
        var allByteValues = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();

        // Act
        var result = _service.Encode(allByteValues);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var decodedBytes = Convert.FromBase64String(result.Output);
        decodedBytes.ShouldBe(allByteValues);
    }

    [Fact]
    public void Encode_ReturnValidBase64_WhenInputIsSingleByte()
    {
        // Arrange
        var singleByte = new byte[] { 65 }; // 'A'

        // Act
        var result = _service.Encode(singleByte);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Output.ShouldBe("QQ==");
    }

    #endregion
}
