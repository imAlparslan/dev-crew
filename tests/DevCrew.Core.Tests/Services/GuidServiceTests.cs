using System;
using System.Collections.Generic;
using DevCrew.Core.Application.Services;
using DevCrew.Core.Infrastructure.Persistence.Repositories;
using NSubstitute;
using Shouldly;
using Xunit;

namespace DevCrew.Core.Tests.Services;

public sealed class GuidServiceTests
{
    private readonly GuidService _service;

    public GuidServiceTests()
    {
        var guidRepository = Substitute.For<IGuidRepository>();
        _service = new GuidService(guidRepository);
    }

    [Fact]
    public void Generate_ReturnValidGuid_WhenCalled()
    {
        // Act
        var result = _service.Generate();

        // Assert
        result.ShouldNotBeNullOrWhiteSpace();
        Guid.TryParse(result, out var guidResult).ShouldBeTrue();
        guidResult.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Generate_ReturnUniqueGuid_WhenCalledMultipleTimes()
    {
        // Arrange
        var guids = new HashSet<string>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            guids.Add(_service.Generate());
        }

        // Assert
        guids.Count.ShouldBe(100);
    }

    [Fact]
    public void Generate_ReturnValidGuidFormat_WhenCalled()
    {
        // Act
        var result = _service.Generate();

        // Assert
        // GuidService returns default format (with hyphens, no braces)
        result.ShouldMatch(@"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$");
    }
}
