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
}
