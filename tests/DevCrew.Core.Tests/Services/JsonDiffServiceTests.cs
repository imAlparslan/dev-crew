using Xunit;
using DevCrew.Core.Services;

namespace DevCrew.Core.Tests.Services;

public class JsonDiffServiceTests
{
    [Fact]
    public void Test_NullLeftJson_ReturnsInvalid()
    {
        var service = new JsonDiffService();
        var rightJson = "{\"key\": \"value\"}";
        var result = service.Compare(null!, rightJson);
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void Test_NullRightJson_ReturnsInvalid()
    {
        var service = new JsonDiffService();
        var leftJson = "{\"key\": \"value\"}";
        var result = service.Compare(leftJson, null!);
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void Test_InvalidLeftJson_ReturnsError()
    {
        var service = new JsonDiffService();
        var leftJson = "{invalid json}";
        var rightJson = "{\"key\": \"value\"}";
        var result = service.Compare(leftJson, rightJson);
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void Test_InvalidRightJson_ReturnsError()
    {
        var service = new JsonDiffService();
        var leftJson = "{\"key\": \"value\"}";
        var rightJson = "{invalid json}";
        var result = service.Compare(leftJson, rightJson);
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void Test_IdenticalObjects_ProducesNoChanges()
    {
        var service = new JsonDiffService();
        var json = "{\"name\": \"John\", \"age\": 30}";
        var result = service.Compare(json, json);
        Assert.True(result.IsValid);
        Assert.Equal(0, result.Summary.TotalDifferences);
        Assert.Empty(result.PathDiffs);
    }

    [Fact]
    public void Test_IdenticalJsonWithDifferentFormatting_IgnoresWhitespace()
    {
        var service = new JsonDiffService();
        var leftJson = "{\"name\": \"John\", \"age\": 30}";
        var rightJson = "{\n  \"name\": \"John\",\n  \"age\": 30\n}";
        var options = new JsonDiffOptions { IgnoreWhitespaceDifferences = true };
        var result = service.Compare(leftJson, rightJson, options);
        Assert.True(result.IsValid);
        Assert.Equal(0, result.Summary.TotalDifferences);
    }

    [Fact]
    public void Test_AddedProperty_ProducesAddedDiff()
    {
        var service = new JsonDiffService();
        var leftJson = "{\"name\": \"John\"}";
        var rightJson = "{\"name\": \"John\", \"age\": 30}";
        var result = service.Compare(leftJson, rightJson);
        Assert.True(result.IsValid);
        Assert.True(result.Summary.AddedCount > 0);
        Assert.Contains(result.PathDiffs, d => d.Kind == JsonDiffKind.Added);
    }

    [Fact]
    public void Test_RemovedProperty_ProducesRemovedDiff()
    {
        var service = new JsonDiffService();
        var leftJson = "{\"name\": \"John\", \"age\": 30}";
        var rightJson = "{\"name\": \"John\"}";
        var result = service.Compare(leftJson, rightJson);
        Assert.True(result.IsValid);
        Assert.True(result.Summary.RemovedCount > 0);
        Assert.Contains(result.PathDiffs, d => d.Kind == JsonDiffKind.Removed);
    }

    [Fact]
    public void Test_ChangedPropertyValue_ProducesChangedDiff()
    {
        var service = new JsonDiffService();
        var leftJson = "{\"name\": \"John\", \"age\": 30}";
        var rightJson = "{\"name\": \"Jane\", \"age\": 30}";
        var result = service.Compare(leftJson, rightJson);
        Assert.True(result.IsValid);
        Assert.True(result.Summary.ChangedCount > 0);
        Assert.Contains(result.PathDiffs, d => d.Kind == JsonDiffKind.Changed);
    }

    [Fact]
    public void Test_IgnorePropertyOrder_True_IgnoresKeyOrder()
    {
        var service = new JsonDiffService();
        var leftJson = "{\"name\": \"John\", \"age\": 30}";
        var rightJson = "{\"age\": 30, \"name\": \"John\"}";
        var options = new JsonDiffOptions { IgnoreObjectPropertyOrder = true };
        var result = service.Compare(leftJson, rightJson, options);
        Assert.True(result.IsValid);
        Assert.Equal(0, result.Summary.TotalDifferences);
    }

    [Fact]
    public void Test_IgnorePropertyOrder_False_DetectsKeyOrder()
    {
        var service = new JsonDiffService();
        var leftJson = "{\"name\": \"John\", \"age\": 30}";
        var rightJson = "{\"age\": 30, \"name\": \"John\"}";
        var options = new JsonDiffOptions { IgnoreObjectPropertyOrder = false };
        var result = service.Compare(leftJson, rightJson, options);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Test_ArrayOrderSignificant_True_DetectsReordering()
    {
        var service = new JsonDiffService();
        var leftJson = "{\"items\": [1, 2, 3]}";
        var rightJson = "{\"items\": [3, 2, 1]}";
        var options = new JsonDiffOptions { TreatArrayOrderAsSignificant = true };
        var result = service.Compare(leftJson, rightJson, options);
        Assert.True(result.IsValid);
        Assert.True(result.Summary.TotalDifferences > 0);
    }

    [Fact]
    public void Test_ArrayOrderSignificant_False_IgnoresSetOrder()
    {
        var service = new JsonDiffService();
        var leftJson = "{\"items\": [1, 2, 3]}";
        var rightJson = "{\"items\": [3, 2, 1]}";
        var options = new JsonDiffOptions { TreatArrayOrderAsSignificant = false };
        var result = service.Compare(leftJson, rightJson, options);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Test_TreatNullAndEmptyStringAsEqual_True_NoDiff()
    {
        var service = new JsonDiffService();
        var leftJson = "{\"value\": null}";
        var rightJson = "{\"value\": \"\"}";
        var options = new JsonDiffOptions { TreatNullAndEmptyStringAsEqual = true };
        var result = service.Compare(leftJson, rightJson, options);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Test_TreatNullAndEmptyStringAsEqual_False_DetectsDiff()
    {
        var service = new JsonDiffService();
        var leftJson = "{\"value\": null}";
        var rightJson = "{\"value\": \"\"}";
        var options = new JsonDiffOptions { TreatNullAndEmptyStringAsEqual = false };
        var result = service.Compare(leftJson, rightJson, options);
        Assert.True(result.IsValid);
        Assert.True(result.Summary.ChangedCount > 0);
    }

    [Fact]
    public void Test_EmptyObjects_ProducesNoChanges()
    {
        var service = new JsonDiffService();
        var json = "{}";
        var result = service.Compare(json, json);
        Assert.True(result.IsValid);
        Assert.Equal(0, result.Summary.TotalDifferences);
    }

    [Fact]
    public void Test_EmptyArray_ComparedCorrectly()
    {
        var service = new JsonDiffService();
        var json = "{\"items\": []}";
        var result = service.Compare(json, json);
        Assert.True(result.IsValid);
        Assert.Equal(0, result.Summary.TotalDifferences);
    }

    [Fact]
    public void Test_NestedObjectDiff_DetectsDeepChanges()
    {
        var service = new JsonDiffService();
        var leftJson = "{\"user\": {\"name\": \"John\", \"address\": {\"city\": \"NYC\"}}}";
        var rightJson = "{\"user\": {\"name\": \"John\", \"address\": {\"city\": \"LA\"}}}";
        var result = service.Compare(leftJson, rightJson);
        Assert.True(result.IsValid);
        Assert.True(result.Summary.ChangedCount > 0);
        Assert.Contains(result.PathDiffs, d => d.Path.Contains("city"));
    }

    [Fact]
    public void Test_LineDiff_OutputMatchesKindEnum()
    {
        var service = new JsonDiffService();
        var leftJson = "{\"a\": 1, \"b\": 2}";
        var rightJson = "{\"a\": 1, \"b\": 3}";
        var result = service.Compare(leftJson, rightJson);
        Assert.True(result.IsValid);
        Assert.NotEmpty(result.LineDiffs);
        foreach (var ld in result.LineDiffs)
        {
            Assert.True(ld.Kind == JsonDiffKind.Added || ld.Kind == JsonDiffKind.Removed 
                || ld.Kind == JsonDiffKind.Changed || ld.Kind == JsonDiffKind.Unchanged);
        }
    }
}
