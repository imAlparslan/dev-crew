using System.Text;
using DevCrew.Core.Application.Services;
using DevCrew.Core.Infrastructure.Persistence.Repositories;
using NSubstitute;
using Shouldly;
using Xunit;

namespace DevCrew.Core.Tests.Services;

/// <summary>
/// Integration tests for cross-service interactions and complex workflows
/// </summary>
public class ServiceIntegrationTests
{
    private readonly JsonFormatterService _jsonFormatterService;
    private readonly Base64EncoderService _base64EncoderService;
    private readonly GuidService _guidService;

    public ServiceIntegrationTests()
    {
        _jsonFormatterService = new JsonFormatterService();
        _base64EncoderService = new Base64EncoderService();
        var guidRepository = Substitute.For<IGuidRepository>();
        _guidService = new GuidService(guidRepository);
    }

    #region Base64 + JSON Formatter Integration

    [Fact]
    public void Base64AndJsonFormatter_WorkTogether_WhenEncodingJsonPayload()
    {
        // Arrange
        var jsonPayload = "{\"user\": \"john\", \"email\": \"john@example.com\"}";
        var jsonBytes = Encoding.UTF8.GetBytes(jsonPayload);

        // Act - Encode JSON to Base64
        var encodeResult = _base64EncoderService.Encode(jsonBytes);
        encodeResult.IsSuccess.ShouldBeTrue();

        // Act - Decode back and validate with JsonFormatter
        var decodeResult = _base64EncoderService.Decode(encodeResult.Output);
        decodeResult.IsSuccess.ShouldBeTrue();

        var decodedJson = Encoding.UTF8.GetString(decodeResult.Output);
        var validateResult = _jsonFormatterService.Validate(decodedJson);

        // Assert
        validateResult.IsValid.ShouldBeTrue();
        decodedJson.ShouldBe(jsonPayload);
    }

    [Fact]
    public void Base64JsonRoundtrip_PreserveUnicodeAndStructure_WhenProcessingComplexJson()
    {
        // Arrange
        var complexJson = "{\"greeting\": \"Hello 👋\", \"items\": [1, 2, 3]}";
        var jsonBytes = Encoding.UTF8.GetBytes(complexJson);

        // Act - Full cycle: JSON -> Base64 -> Decode -> Validate
        var encoded = _base64EncoderService.Encode(jsonBytes);
        var decoded = _base64EncoderService.Decode(encoded.Output);
        var decodedJson = Encoding.UTF8.GetString(decoded.Output);
        var validated = _jsonFormatterService.Validate(decodedJson);

        // Assert
        validated.IsValid.ShouldBeTrue();
        encoded.IsSuccess.ShouldBeTrue();
        decoded.IsSuccess.ShouldBeTrue();
    }

    #endregion

    #region GUID + JSON Formatter Integration

    [Fact]
    public void GuidAndJsonFormatter_CreateJsonWithGuid_WhenGeneratingIdentifier()
    {
        // Arrange
        var guid = _guidService.Generate();

        // Act - Create JSON with GUID
        var json = $"{{\"id\": \"{guid}\", \"created\": true}}";
        var validateResult = _jsonFormatterService.Validate(json);
        var prettifyResult = _jsonFormatterService.Prettify(json);

        // Assert
        validateResult.IsValid.ShouldBeTrue();
        prettifyResult.IsValid.ShouldBeTrue();
        prettifyResult.Output.ShouldContain(guid);
    }

    #endregion

    #region Base64 + GUID Integration

    [Fact]
    public void Base64AndGuid_EncodeGuidAsJson_WhenStoringIdentifiers()
    {
        // Arrange
        var guid = _guidService.Generate();
        var guidJson = $"{{\"identifier\": \"{guid}\"}}";
        var guidBytes = Encoding.UTF8.GetBytes(guidJson);

        // Act
        var encodeResult = _base64EncoderService.Encode(guidBytes);
        encodeResult.IsSuccess.ShouldBeTrue();

        // Act - Decode and verify
        var decodeResult = _base64EncoderService.Decode(encodeResult.Output);
        decodeResult.IsSuccess.ShouldBeTrue();

        var decodedJson = Encoding.UTF8.GetString(decodeResult.Output);

        // Assert
        decodedJson.ShouldContain(guid);
    }

    #endregion

    #region JSON Formatter Complex Workflows

    [Fact]
    public void JsonFormatter_ValidatePrettifyMinify_WorkCorrectlyInSequence()
    {
        // Arrange
        var minifiedJson = "{\"name\":\"test\",\"value\":123}";

        // Act
        var validateResult = _jsonFormatterService.Validate(minifiedJson);
        var prettifyResult = _jsonFormatterService.Prettify(minifiedJson);
        var minifyResult = _jsonFormatterService.Minify(prettifyResult.Output);

        // Assert
        validateResult.IsValid.ShouldBeTrue();
        prettifyResult.IsValid.ShouldBeTrue();
        minifyResult.IsValid.ShouldBeTrue();
        minifyResult.Output.ShouldBe(minifiedJson);
    }

    [Fact]
    public void JsonFormatter_SortKeysAndPrettify_ProduceDeterministicOutput()
    {
        // Arrange
        var unsortedJson = "{\"z\": 1, \"a\": 2, \"m\": 3}";

        // Act
        var sortResult = _jsonFormatterService.SortKeys(unsortedJson);
        sortResult.IsValid.ShouldBeTrue();

        var prettifyResult = _jsonFormatterService.Prettify(sortResult.Output);
        prettifyResult.IsValid.ShouldBeTrue();

        // Second execution should produce identical output (deterministic)
        var sortResult2 = _jsonFormatterService.SortKeys(unsortedJson);
        var prettifyResult2 = _jsonFormatterService.Prettify(sortResult2.Output);

        // Assert
        prettifyResult.Output.ShouldBe(prettifyResult2.Output);
    }

    #endregion
}
