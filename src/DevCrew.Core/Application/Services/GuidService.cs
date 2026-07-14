using System.Text;
using DevCrew.Core.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace DevCrew.Core.Application.Services;

/// <summary>
/// Default GUID generation service.
/// </summary>
public class GuidService(IServiceScopeFactory scopeFactory) : IGuidService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    /// <inheritdoc/>
    public string Generate() => Guid.NewGuid().ToString();
    /// <inheritdoc/>
    public async Task<string> DeleteGuidByValueAndNotes(string? value, string? notes, CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IGuidRepository>();

        var guids = await repository.GetGuidByValueAndNotes(value, notes, cancellationToken, maxResults: 4);
        if (guids.Count == 0)
            return $"No GUIDs found matching the specified criteria.";

        if (guids.Count > 1)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Multiple GUIDs found;");
            guids.Take(3).ToList().ForEach(g => stringBuilder.AppendLine($"Value: {g.GuidValue}, Notes: {g.Notes}"));
            if (guids.Count > 3)
                stringBuilder.AppendLine("...and more.");

            stringBuilder.AppendLine("Please specify a more specific value.");

            return stringBuilder.ToString();
        }
        var guidToDelete = guids[0];
        await repository.DeleteGuidAsync(guidToDelete.Id, cancellationToken);
        return $"Deleted GUID: {guidToDelete.GuidValue}";
    }
}
