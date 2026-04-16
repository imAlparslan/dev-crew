using System.Text.RegularExpressions;
using DevCrew.Core.Domain.Results;
using DevCrew.Core.Shared.Constants;

namespace DevCrew.Core.Application.Services;

public sealed class RegexService : IRegexService
{
    public RegexMatchResult FindMatches(string pattern, string input, bool ignoreCase = false, bool multiline = false)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return new RegexMatchResult
            {
                IsValid = false,
                ErrorMessage = "Regex pattern is required",
                ErrorKey = ErrorKeys.Regex.PatternRequired
            };
        }

        input ??= string.Empty;

        try
        {
            var regex = new Regex(pattern, BuildOptions(ignoreCase, multiline), TimeSpan.FromSeconds(2));
            var groupNames = regex.GetGroupNames();
            var matches = regex.Matches(input);
            var items = new List<RegexMatchItem>(matches.Count);

            foreach (Match match in matches)
            {
                if (!match.Success)
                {
                    continue;
                }

                var captures = new List<RegexCaptureItem>();
                for (var groupIndex = 1; groupIndex < match.Groups.Count; groupIndex++)
                {
                    var group = match.Groups[groupIndex];
                    if (!group.Success)
                    {
                        continue;
                    }

                    var groupName = groupIndex < groupNames.Length ? groupNames[groupIndex] : groupIndex.ToString();
                    captures.Add(new RegexCaptureItem
                    {
                        Name = groupName,
                        Value = group.Value,
                        Index = group.Index,
                        Length = group.Length,
                        IsNamed = !string.Equals(groupName, groupIndex.ToString(), StringComparison.Ordinal)
                    });
                }

                items.Add(new RegexMatchItem
                {
                    Index = match.Index,
                    Length = match.Length,
                    Value = match.Value,
                    Captures = captures
                });
            }

            return new RegexMatchResult
            {
                IsValid = true,
                Pattern = pattern,
                IgnoreCase = ignoreCase,
                Multiline = multiline,
                InputLength = input.Length,
                Matches = items
            };
        }
        catch (RegexParseException ex)
        {
            return new RegexMatchResult
            {
                IsValid = false,
                ErrorMessage = $"Invalid regex pattern: {ex.Message}",
                ErrorKey = ErrorKeys.Regex.InvalidPattern,
                ErrorArgs = [ex.Message]
            };
        }
        catch (ArgumentException ex)
        {
            return new RegexMatchResult
            {
                IsValid = false,
                ErrorMessage = $"Invalid regex pattern: {ex.Message}",
                ErrorKey = ErrorKeys.Regex.InvalidPattern,
                ErrorArgs = [ex.Message]
            };
        }
        catch (RegexMatchTimeoutException ex)
        {
            return new RegexMatchResult
            {
                IsValid = false,
                ErrorMessage = $"Regex matching timed out: {ex.Message}",
                ErrorKey = ErrorKeys.Regex.MatchTimeout,
                ErrorArgs = [ex.Message]
            };
        }
        catch (Exception ex)
        {
            return new RegexMatchResult
            {
                IsValid = false,
                ErrorMessage = $"Regex processing failed: {ex.Message}",
                ErrorKey = ErrorKeys.Regex.ProcessingFailed,
                ErrorArgs = [ex.Message]
            };
        }
    }

    private static RegexOptions BuildOptions(bool ignoreCase, bool multiline)
    {
        var options = RegexOptions.CultureInvariant;

        if (ignoreCase)
        {
            options |= RegexOptions.IgnoreCase;
        }

        if (multiline)
        {
            options |= RegexOptions.Multiline;
        }

        return options;
    }
}
