using System;
using DevCrew.Core.Services;
using Shouldly;
using Xunit;

namespace DevCrew.Core.Tests.Services;

public class JsonDiffServiceTests
{
    [Fact]
    public void Compare_NullLeftJson_ReturnsInvalidResult()
    {
        var service = new JsonDiffService();
        var rightJson = "{\"key\": \"value\"}";

        var result = service.Compare(null!, rightJson);

        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull();
    }

    [Fact]
    public void Compare_NullRightJson_ReturnsInvalidResult()
    {
        var service = new JsonDiffService();
        var leftJson = "{\"key\": \"value\"}";

        var result = service.Compare(leftJson, null!);

        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull();
    }

    [Fact]
    public void Compare_InvalidLeftJson_ReturnsError()
    {
        var service = new JsonDiffService();
        var leftJson = "{invalid json}";
        var rightJson = "{\"key\": \"value\"}";

        var result = service.Compare(leftJson, rightJson);

        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull();
    }

    [Fact]
    public void Compare_InvalidRightJson_ReturnsError()
    {
        var service = new JsonDiffService();
        var leftJson = "{\"key\": \"value\"}";
        var rightJson = "{invalid json}";

        var result = service.Compare(leftJson, rightJson);

        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull();
    }

    [Fact]
    public void Compare_IdenticalObjects_ProducesNoChanges()
    {
        var service = new JsonDiffService();
        var json = "{\"name\": \"John\", \"age\": 30}";

        var result = service.Compare(json, json);

        result.IsValid.ShouldBeTrue();
        result.Summary.TotalDifferences.ShouldBe(0);
        result.PathDiffs.ShouldBeEmpty();
    }

    [Fact]
    public void Compare_IdenticalJsonWithDifferentFormattingAndWhitespaceIgnored_ProducesNoDifferences()
    {
        var service = new JsonDiffService();
        var leftJson = "{\"name\": \"John\", \"age\": 30}";
        var rightJson = "{\n  \"name\": \"John\",\n  \"age\": 30\n}";
        var options = new JsonDiffOptions { IgnoreWhitespaceDifferences = true };

        var result = service.Compare(leftJson, rightJson, options);

        result.IsValid.ShouldBeTrue();
        result.Summary.TotalDifferences.ShouldBe(0);
    }

    [Fact]
    public void Compare_AddedProperty_ProducesAddedDiff()
    {
        var service = new JsonDiffService();
        var leftJson = "{\"name\": \"John\"}";
        var rightJson = "{\"name\": \"John\", \"age\": 30}";

        var result = service.Compare(leftJson, rightJson);

        result.IsValid.ShouldBeTrue();
        result.Summary.AddedCount.ShouldBeGreaterThan(0);
        result.PathDiffs.ShouldContain(d => d.Kind == JsonDiffKind.Added);
    }

    [Fact]
    public void Compare_RemovedProperty_ProducesRemovedDiff()
    {
        var service = new JsonDiffService();
        var leftJson = "{\"name\": \"John\", \"age\": 30}";
        var rightJson = "{\"name\": \"John\"}";

        var result = service.Compare(leftJson, rightJson);

        result.IsValid.ShouldBeTrue();
        result.Summary.RemovedCount.ShouldBeGreaterThan(0);
        result.PathDiffs.ShouldContain(d => d.Kind == JsonDiffKind.Removed);
    }

    [Fact]
    public void Compare_ChangedPropertyValue_ProducesChangedDiff()
    {
        var service = new JsonDiffService();
        var leftJson = "{\"name\": \"John\", \"age\": 30}";
        var rightJson = "{\"name\": \"Jane\", \"age\": 30}";

        var result = service.Compare(leftJson, rightJson);

        result.IsValid.ShouldBeTrue();
        result.Summary.ChangedCount.ShouldBeGreaterThan(0);
        result.PathDiffs.ShouldContain(d => d.Kind == JsonDiffKind.Changed);
    }

    [Fact]
    public void Compare_IgnoreObjectPropertyOrderTrue_IgnoresKeyOrder()
    {
        var service = new JsonDiffService();
        var leftJson = "{\"name\": \"John\", \"age\": 30}";
        var rightJson = "{\"age\": 30, \"name\": \"John\"}";
        var options = new JsonDiffOptions { IgnoreObjectPropertyOrder = true };

        var result = service.Compare(leftJson, rightJson, options);

        result.IsValid.ShouldBeTrue();
        result.Summary.TotalDifferences.ShouldBe(0);
    }

    [Fact]
    public void Compare_IgnoreObjectPropertyOrderFalse_DetectsKeyReordering()
    {
        var service = new JsonDiffService();
        var leftJson = "{\"name\": \"John\", \"age\": 30}";
        var rightJson = "{\"age\": 30, \"name\": \"John\"}";
        var options = new JsonDiffOptions { IgnoreObjectPropertyOrder = false };

        var result = service.Compare(leftJson, rightJson, options);

        result.IsValid.ShouldBeTrue();
        result.Summary.TotalDifferences.ShouldBeGreaterThan(0);
        result.PathDiffs.ShouldContain(d => d.Kind == JsonDiffKind.Added);
        result.PathDiffs.ShouldContain(d => d.Kind == JsonDiffKind.Removed);
    }

    [Fact]
    public void Compare_ArrayOrderSignificantTrue_DetectsReordering()
    {
        var service = new JsonDiffService();
        var leftJson = "{\"items\": [1, 2, 3]}";
        var rightJson = "{\"items\": [3, 2, 1]}";
        var options = new JsonDiffOptions { TreatArrayOrderAsSignificant = true };

        var result = service.Compare(leftJson, rightJson, options);

        result.IsValid.ShouldBeTrue();
        result.Summary.TotalDifferences.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Compare_ArrayOrderSignificantFalse_IgnoresSetOrder()
    {
        var service = new JsonDiffService();
        var leftJson = "{\"items\": [1, 2, 3]}";
        var rightJson = "{\"items\": [3, 2, 1]}";
        var options = new JsonDiffOptions { TreatArrayOrderAsSignificant = false };

        var result = service.Compare(leftJson, rightJson, options);

        result.IsValid.ShouldBeTrue();
        result.Summary.TotalDifferences.ShouldBe(0);
    }

    [Fact]
    public void Compare_UnorderedArrayWithDuplicateCounts_ReportsAddedAndRemoved()
    {
        var service = new JsonDiffService();
        var leftJson = "{\"items\": [1, 1, 2]}";
        var rightJson = "{\"items\": [1, 2, 2]}";
        var options = new JsonDiffOptions { TreatArrayOrderAsSignificant = false };

        var result = service.Compare(leftJson, rightJson, options);

        result.IsValid.ShouldBeTrue();
        result.Summary.RemovedCount.ShouldBe(1);
        result.Summary.AddedCount.ShouldBe(1);
        result.PathDiffs.ShouldContain(d => d.Path == "$.items[*]" && d.Kind == JsonDiffKind.Added);
        result.PathDiffs.ShouldContain(d => d.Path == "$.items[*]" && d.Kind == JsonDiffKind.Removed);
    }

    [Fact]
    public void Compare_TreatNullAndEmptyStringAsEqualTrue_NoDiff()
    {
        var service = new JsonDiffService();
        var leftJson = "{\"value\": null}";
        var rightJson = "{\"value\": \"\"}";
        var options = new JsonDiffOptions { TreatNullAndEmptyStringAsEqual = true };

        var result = service.Compare(leftJson, rightJson, options);

        result.IsValid.ShouldBeTrue();
        result.Summary.TotalDifferences.ShouldBe(0);
    }

    [Fact]
    public void Compare_TreatNullAndEmptyStringAsEqualFalse_DetectsDiff()
    {
        var service = new JsonDiffService();
        var leftJson = "{\"value\": null}";
        var rightJson = "{\"value\": \"\"}";
        var options = new JsonDiffOptions { TreatNullAndEmptyStringAsEqual = false };

        var result = service.Compare(leftJson, rightJson, options);

        result.IsValid.ShouldBeTrue();
        result.Summary.ChangedCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Compare_NestedNullAndEmptyStringAsEqualTrue_TreatsValuesAsEqual()
    {
        var service = new JsonDiffService();
        var leftJson = "{\"user\": {\"name\": \"\", \"aliases\": [null]}}";
        var rightJson = "{\"user\": {\"name\": null, \"aliases\": [\"\"]}}";
        var options = new JsonDiffOptions { TreatNullAndEmptyStringAsEqual = true };

        var result = service.Compare(leftJson, rightJson, options);

        result.IsValid.ShouldBeTrue();
        result.Summary.TotalDifferences.ShouldBe(0);
    }

    [Fact]
    public void Compare_NestedNullAndEmptyStringAsEqualFalse_ReportsDiffs()
    {
        var service = new JsonDiffService();
        var leftJson = "{\"user\": {\"name\": \"\", \"aliases\": [null]}}";
        var rightJson = "{\"user\": {\"name\": null, \"aliases\": [\"\"]}}";
        var options = new JsonDiffOptions { TreatNullAndEmptyStringAsEqual = false };

        var result = service.Compare(leftJson, rightJson, options);

        result.IsValid.ShouldBeTrue();
        result.Summary.ChangedCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Compare_EmptyObjects_ProducesNoChanges()
    {
        var service = new JsonDiffService();
        var json = "{}";

        var result = service.Compare(json, json);

        result.IsValid.ShouldBeTrue();
        result.Summary.TotalDifferences.ShouldBe(0);
    }

    [Fact]
    public void Compare_EmptyArray_ComparedCorrectly()
    {
        var service = new JsonDiffService();
        var json = "{\"items\": []}";

        var result = service.Compare(json, json);

        result.IsValid.ShouldBeTrue();
        result.Summary.TotalDifferences.ShouldBe(0);
    }

    [Fact]
    public void Compare_NestedObjectDiff_DetectsDeepChanges()
    {
        var service = new JsonDiffService();
        var leftJson = "{\"user\": {\"name\": \"John\", \"address\": {\"city\": \"NYC\"}}}";
        var rightJson = "{\"user\": {\"name\": \"John\", \"address\": {\"city\": \"LA\"}}}";

        var result = service.Compare(leftJson, rightJson);

        result.IsValid.ShouldBeTrue();
        result.Summary.ChangedCount.ShouldBeGreaterThan(0);
        result.PathDiffs.ShouldContain(d => d.Path.Contains("city", StringComparison.Ordinal));
    }

    [Fact]
    public void Compare_NumberAndStringTypeMismatch_ReportsChangedDiff()
    {
        var service = new JsonDiffService();
        var leftJson = "{\"value\": 1}";
        var rightJson = "{\"value\": \"1\"}";

        var result = service.Compare(leftJson, rightJson);

        result.IsValid.ShouldBeTrue();
        result.Summary.ChangedCount.ShouldBeGreaterThan(0);
        result.PathDiffs.ShouldContain(d => d.Path == "$.value" && d.Kind == JsonDiffKind.Changed);
    }

    [Fact]
    public void Compare_ObjectAndArrayTypeMismatch_ReportsChangedDiff()
    {
        var service = new JsonDiffService();
        var leftJson = "{\"value\": {\"a\": 1}}";
        var rightJson = "{\"value\": [1]}";

        var result = service.Compare(leftJson, rightJson);

        result.IsValid.ShouldBeTrue();
        result.Summary.ChangedCount.ShouldBeGreaterThan(0);
        result.PathDiffs.ShouldContain(d => d.Path == "$.value" && d.Kind == JsonDiffKind.Changed);
    }

    [Fact]
    public void Compare_IgnoreWhitespaceDifferencesFalse_DetectsFormattingChangeInLineDiffs()
    {
        var service = new JsonDiffService();
        var leftJson = "{\"name\":\"John\",\"age\":30}";
        var rightJson = "{\n  \"name\": \"John\",\n  \"age\": 30\n}";
        var options = new JsonDiffOptions { IgnoreWhitespaceDifferences = false };

        var result = service.Compare(leftJson, rightJson, options);

        result.IsValid.ShouldBeTrue();
        result.Summary.TotalDifferences.ShouldBe(0);
        result.LineDiffs.ShouldContain(d => d.Kind != JsonDiffKind.Unchanged);
    }

    [Fact]
    public void Compare_SpecialPropertyName_UsesBracketPathNotation()
    {
        var service = new JsonDiffService();
        var leftJson = "{\"my.prop\": 1}";
        var rightJson = "{\"my.prop\": 2}";

        var result = service.Compare(leftJson, rightJson);

        result.IsValid.ShouldBeTrue();
        result.PathDiffs.ShouldContain(d => d.Path == "$[\"my.prop\"]" && d.Kind == JsonDiffKind.Changed);
    }

    [Fact]
    public void Compare_LineDiffOutput_ContainsKnownKindsOnly()
    {
        var service = new JsonDiffService();
        var leftJson = "{\"a\": 1, \"b\": 2}";
        var rightJson = "{\"a\": 1, \"b\": 3}";

        var result = service.Compare(leftJson, rightJson);

        result.IsValid.ShouldBeTrue();
        result.LineDiffs.ShouldNotBeEmpty();

        foreach (var lineDiff in result.LineDiffs)
        {
            (lineDiff.Kind == JsonDiffKind.Added
             || lineDiff.Kind == JsonDiffKind.Removed
             || lineDiff.Kind == JsonDiffKind.Changed
             || lineDiff.Kind == JsonDiffKind.Unchanged).ShouldBeTrue();
        }
    }
}
