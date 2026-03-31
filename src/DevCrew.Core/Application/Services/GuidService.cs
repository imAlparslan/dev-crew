namespace DevCrew.Core.Application.Services;

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

