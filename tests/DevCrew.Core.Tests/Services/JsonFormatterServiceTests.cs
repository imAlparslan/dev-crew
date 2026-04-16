using System.Text.Json;
using DevCrew.Core.Application.Services;
using Shouldly;
using Xunit;

namespace DevCrew.Core.Tests.Services;

public sealed class JsonFormatterServiceTests
{
    private readonly JsonFormatterService _service = new();

    #region Validate Tests

    [Fact]
    public void Validate_ReturnTrue_WhenJsonIsValid()
    {
        // Arrange
        var validJson = "{\"name\": \"John\", \"age\": 30}";

        // Act
        var result = _service.Validate(validJson);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Output.ShouldBe(validJson);
    }

    [Fact]
    public void Validate_ReturnFalse_WhenJsonIsInvalid()
    {
        // Arrange
        var invalidJson = "{name: John}";

        // Act
        var result = _service.Validate(invalidJson);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorKey.ShouldBe("jsonformatter.invalid_json");
        result.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Validate_ReturnFalse_WhenInputIsNull()
    {
        // Arrange
        string? nullInput = null;

        // Act
        var result = _service.Validate(nullInput);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorKey.ShouldBe("jsonformatter.input_required");
    }

    [Fact]
    public void Validate_ReturnFalse_WhenInputIsEmpty()
    {
        // Arrange
        var emptyInput = string.Empty;

        // Act
        var result = _service.Validate(emptyInput);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorKey.ShouldBe("jsonformatter.input_required");
    }

    #endregion

    #region Prettify Tests

    [Fact]
    public void Prettify_ReturnFormattedJson_WhenInputIsValid()
    {
        // Arrange
        var minifiedJson = "{\"name\":\"John\",\"age\":30}";

        // Act
        var result = _service.Prettify(minifiedJson);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Output.ShouldNotBeNullOrWhiteSpace();
        result.Output.ShouldContain("\n");
        result.Output.ShouldContain("  ");
    }

    [Fact]
    public void Prettify_ReturnSortedJson_WhenSortKeysIsEnabled()
    {
        // Arrange
        var unsortedJson = "{\"z\":1,\"a\":2,\"m\":3}";

        // Act
        var result = _service.Prettify(unsortedJson, sortKeys: true);

        // Assert
        result.IsValid.ShouldBeTrue();
        var output = result.Output;
        var aIndex = output.IndexOf("\"a\"");
        var mIndex = output.IndexOf("\"m\"");
        var zIndex = output.IndexOf("\"z\"");
        aIndex.ShouldBeLessThan(mIndex);
        mIndex.ShouldBeLessThan(zIndex);
    }

    [Fact]
    public void Prettify_ReturnFalse_WhenInputIsInvalid()
    {
        // Arrange
        var invalidJson = "{invalid json}";

        // Act
        var result = _service.Prettify(invalidJson);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorKey.ShouldBe("jsonformatter.invalid_json");
    }

    #endregion

    #region Minify Tests

    [Fact]
    public void Minify_RemoveWhitespace_WhenInputIsValid()
    {
        // Arrange
        var prettyJson = "{\n  \"name\": \"John\",\n  \"age\": 30\n}";

        // Act
        var result = _service.Minify(prettyJson);

        // Assert
        result.IsValid.ShouldBeTrue();
        var output = result.Output;
        output.ShouldNotContain("\n");
        output.ShouldNotContain("  ");
        output.ShouldStartWith("{");
        output.ShouldEndWith("}");
    }

    [Fact]
    public void Minify_PreserveUnicode_WhenInputContainsEmoji()
    {
        // Arrange
        var jsonWithEmoji = "{\"greeting\": \"Hello 👋\"}";

        // Act
        var result = _service.Minify(jsonWithEmoji);

        // Assert
        result.IsValid.ShouldBeTrue();
        // JSON serializer may escape unicode, so check that the decoded value contains emoji
        result.Output.ShouldNotBeNullOrWhiteSpace();
        // Decode to verify emoji is preserved in the actual data
        var decoded = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string>>(result.Output);
        decoded.ShouldNotBeNull();
        decoded["greeting"].ShouldContain("👋");
    }

    [Fact]
    public void Minify_ReturnFalse_WhenInputIsInvalid()
    {
        // Arrange
        var invalidJson = "[unclosed array";

        // Act
        var result = _service.Minify(invalidJson);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorKey.ShouldBe("jsonformatter.invalid_json");
    }

    #endregion

    #region SortKeys Tests

    [Fact]
    public void SortKeys_ReturnSortedJson_WhenInputIsValid()
    {
        // Arrange
        var unsortedJson = "{\"z\":\"last\",\"a\":\"first\",\"m\":\"middle\"}";

        // Act
        var result = JsonFormatterService.SortKeys(unsortedJson);

        // Assert
        result.IsValid.ShouldBeTrue();
        var output = result.Output;
        var aIndex = output.IndexOf("\"a\"");
        var mIndex = output.IndexOf("\"m\"");
        var zIndex = output.IndexOf("\"z\"");
        aIndex.ShouldBeLessThan(mIndex);
        mIndex.ShouldBeLessThan(zIndex);
    }

    [Fact]
    public void SortKeys_ReturnFalse_WhenInputIsInvalid()
    {
        // Arrange
        var invalidJson = "{bad json}";

        // Act
        var result = JsonFormatterService.SortKeys(invalidJson);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorKey.ShouldBe("jsonformatter.invalid_json");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Validate_ReturnTrue_WhenInputIsComplexNestedJson()
    {
        // Arrange
        var complexJson = """
        {
          "users": [
            {
              "id": 1,
              "name": "John",
              "tags": ["admin", "user"]
            },
            {
              "id": 2,
              "name": "Jane",
              "tags": ["user"]
            }
          ],
          "metadata": {
            "total": 2,
            "page": 1
          }
        }
        """;

        // Act
        var result = _service.Validate(complexJson);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Prettify_ReturnValid_WhenInputIsJsonArray()
    {
        // Arrange
        var arrayJson = "[1,2,3,4,5]";

        // Act
        var result = _service.Prettify(arrayJson);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Output.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Minify_ReturnValid_WhenInputIsSimpleString()
    {
        // Arrange
        var simpleJson = "\"hello world\"";

        // Act
        var result = _service.Minify(simpleJson);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Output.ShouldBe("\"hello world\"");
    }

    #endregion
}
