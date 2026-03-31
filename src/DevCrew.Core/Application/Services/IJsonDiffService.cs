using System.Text.Json;
using DevCrew.Core.Domain.Results;
using DevCrew.Core.Shared.Enums;

namespace DevCrew.Core.Application.Services;

public interface IJsonDiffService
{
    JsonDiffResult Compare(string leftJson, string rightJson, JsonDiffOptions? options = null);
}
