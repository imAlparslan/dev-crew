using DevCrew.Core.Domain.Results;

namespace DevCrew.Core.Application.Services;

public interface IRegexService
{
    RegexMatchResult FindMatches(string pattern, string input, bool ignoreCase = false, bool multiline = false);
}
