using System.Text.Json;
using DevCrew.Core.Models.Results;
using DevCrew.Core.Enums;

namespace DevCrew.Core.Services;

public interface IJsonDiffService
{
    JsonDiffResult Compare(string leftJson, string rightJson, JsonDiffOptions? options = null);
}
