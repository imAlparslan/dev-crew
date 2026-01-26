using System;

namespace DevCrew.Core.Services;

public interface IGuidService
{
    string Generate();
}

public class GuidService : IGuidService
{
    public string Generate() => Guid.NewGuid().ToString();
}
