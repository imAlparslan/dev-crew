using DevCrew.Core.Domain.Results;

namespace DevCrew.Core.Application.Services;

public interface IJsonDiffService
{
    JsonDiffResult Compare(string leftJson, string rightJson, JsonDiffOptions? options = null);
}
