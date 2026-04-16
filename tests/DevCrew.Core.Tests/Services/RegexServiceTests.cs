using DevCrew.Core.Application.Services;
using Shouldly;
using Xunit;

namespace DevCrew.Core.Tests.Services;

public sealed class RegexServiceTests
{
    private readonly RegexService _service = new();

    [Fact]
    public void FindMatches_EmptyPattern_ReturnsInvalidResult()
    {
        var result = _service.FindMatches(string.Empty, "sample");

        result.IsValid.ShouldBeFalse();
        result.ErrorKey.ShouldBe("regex.pattern_required");
        result.MatchCount.ShouldBe(0);
    }

    [Fact]
    public void FindMatches_InvalidPattern_ReturnsInvalidResult()
    {
        var result = _service.FindMatches("[abc", "abc");

        result.IsValid.ShouldBeFalse();
        result.ErrorKey.ShouldBe("regex.invalid_pattern");
        result.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void FindMatches_EmptyInput_ReturnsValidEmptyResult()
    {
        var result = _service.FindMatches("test", string.Empty);

        result.IsValid.ShouldBeTrue();
        result.MatchCount.ShouldBe(0);
        result.InputLength.ShouldBe(0);
    }

    [Fact]
    public void FindMatches_MultipleMatches_ReturnsOrderedSpans()
    {
        var result = _service.FindMatches("cat", "cat scatter cat");

        result.IsValid.ShouldBeTrue();
        result.MatchCount.ShouldBe(3);
        result.Matches[0].Index.ShouldBe(0);
        result.Matches[1].Index.ShouldBe(5);
        result.Matches[2].Index.ShouldBe(12);
    }

    [Fact]
    public void FindMatches_IgnoreCaseTrue_MatchesCaseInsensitive()
    {
        var result = _service.FindMatches("hello", "HELLO hello", ignoreCase: true);

        result.IsValid.ShouldBeTrue();
        result.MatchCount.ShouldBe(2);
    }

    [Fact]
    public void FindMatches_MultilineTrue_MatchesLineAnchorsAcrossLines()
    {
        var result = _service.FindMatches("^foo", "foo\nbar\nfoo", multiline: true);

        result.IsValid.ShouldBeTrue();
        result.MatchCount.ShouldBe(2);
        result.Matches[0].Index.ShouldBe(0);
        result.Matches[1].Index.ShouldBe(8);
    }

    [Fact]
    public void FindMatches_NamedGroups_ReturnsNamedCaptures()
    {
        var result = _service.FindMatches("(?<word>\\w+)-(?<digits>\\d+)", "item-42");

        result.IsValid.ShouldBeTrue();
        result.MatchCount.ShouldBe(1);
        result.Matches[0].Captures.Count.ShouldBe(2);
        result.Matches[0].Captures[0].Name.ShouldBe("word");
        result.Matches[0].Captures[0].Value.ShouldBe("item");
        result.Matches[0].Captures[0].IsNamed.ShouldBeTrue();
        result.Matches[0].Captures[1].Name.ShouldBe("digits");
        result.Matches[0].Captures[1].Value.ShouldBe("42");
    }

    [Fact]
    public void FindMatches_UnnamedGroups_ReturnsNumericCaptureNames()
    {
        var result = _service.FindMatches("(ab)(cd)", "abcd");

        result.IsValid.ShouldBeTrue();
        result.MatchCount.ShouldBe(1);
        result.Matches[0].Captures.Count.ShouldBe(2);
        result.Matches[0].Captures[0].Name.ShouldBe("1");
        result.Matches[0].Captures[0].IsNamed.ShouldBeFalse();
        result.Matches[0].Captures[1].Name.ShouldBe("2");
        result.Matches[0].Captures[1].Value.ShouldBe("cd");
    }
}
