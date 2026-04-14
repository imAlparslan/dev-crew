using System;
using DevCrew.Core.Domain.Models;

namespace DevCrew.Core.Tests.Infrastructure.Factories;

public static class RegexPresetTestFactory
{
    public static RegexPreset CreateTestPreset(string name = "Test Preset")
    {
        return new RegexPreset
        {
            Name = name,
            Pattern = @"\d+",
            IgnoreCase = false,
            Multiline = false,
            CreatedAt = DateTime.UtcNow
        };
    }
}
