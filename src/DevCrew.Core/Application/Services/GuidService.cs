using System.Text;
using DevCrew.Core.Infrastructure.Persistence.Repositories;

namespace DevCrew.Core.Application.Services;

/// <summary>
/// Default GUID generation service.
/// </summary>
public class GuidService(IGuidRepository guidRepository) : IGuidService
{
    private readonly IGuidRepository _guidRepository = guidRepository;

    /// <inheritdoc/>
    public string Generate() => Guid.NewGuid().ToString();
    /// <inheritdoc/>
    public async Task<string> DeleteGuidByValueAndNotes(string? value, string? notes, CancellationToken cancellationToken = default)
    {
        var guids = await _guidRepository.GetGuidByValueAndNotes(value, notes, cancellationToken);
        if (guids.Count == 0)
            return $"No GUIDs found matching the specified criteria.";

        if (guids.Count > 1)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"{guids.Count} GUIDs found;");
            guids.Take(3).ToList().ForEach(g => stringBuilder.AppendLine($"Value: {g.GuidValue}, Notes: {g.Notes}"));
            if (guids.Count > 3)
                stringBuilder.AppendLine("...and more.");

            stringBuilder.AppendLine("Please specify a more specific value.");

            return stringBuilder.ToString();
        }
        var guidToDelete = guids[0];
        await _guidRepository.DeleteGuidAsync(guidToDelete.Id, cancellationToken);
        return $"Deleted GUID: {guidToDelete.GuidValue}";
    }
}