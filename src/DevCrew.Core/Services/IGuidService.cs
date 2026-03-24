namespace DevCrew.Core.Services;

/// <summary>
/// Abstraction for GUID generation.
/// </summary>
public interface IGuidService
{
    /// <summary>
    /// Generates a new GUID string.
    /// </summary>
    string Generate();
}
