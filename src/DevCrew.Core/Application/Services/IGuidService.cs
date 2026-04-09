namespace DevCrew.Core.Application.Services;

/// <summary>
/// Abstraction for GUID generation.
/// </summary>
public interface IGuidService
{
    /// <summary>
    /// Generates a new GUID string.
    /// </summary>
    string Generate();
    /// <summary>
    /// Deletes a GUID that starts with the specified value. If multiple GUIDs match, it will return a message indicating that more specific input is needed. If no GUIDs match, it will return a message indicating that no GUIDs were found.
    /// </summary>
    /// <param name="value">The starting value of the GUID to delete</param>
    /// <param name="notes">The notes associated with the GUID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A message indicating the result of the delete operation</returns>
    Task<string> DeleteGuidByValueAndNotes(string value, string notes, CancellationToken cancellationToken = default);
}
