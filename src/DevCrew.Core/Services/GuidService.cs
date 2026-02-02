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

/// <summary>
/// Default GUID generation service.
/// </summary>
public class GuidService : IGuidService
{
    /// <summary>
    /// Generates a new GUID string.
    /// </summary>
    public string Generate() => Guid.NewGuid().ToString();
}

