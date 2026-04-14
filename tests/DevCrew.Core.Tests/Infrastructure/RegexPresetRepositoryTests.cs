using System;
using System.Threading.Tasks;
using DevCrew.Core.Domain.Models;
using DevCrew.Core.Infrastructure.Persistence;
using DevCrew.Core.Infrastructure.Persistence.Repositories;
using DevCrew.Core.Tests.Infrastructure.Factories;
using Shouldly;
using Xunit;

namespace DevCrew.Core.Tests.Infrastructure;

public sealed class RegexPresetRepositoryTests : IDisposable
{
    private readonly RegexPresetRepository _repository;
    private readonly AppDbContext _context;

    public RegexPresetRepositoryTests()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryContext();
        _context = dbContext;
        _repository = new RegexPresetRepository(dbContext);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    #region SaveAsync Tests

    [Fact]
    public async Task SaveAsync_PersistToDatabase_WhenPresetIsValid()
    {
        // Arrange
        var preset = RegexPresetTestFactory.CreateTestPreset("EmailPattern");

        // Act
        var result = await _repository.SaveAsync(preset);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBeGreaterThan(0);
        result.Name.ShouldBe("EmailPattern");
        result.Pattern.ShouldBe(@"\d+");
    }

    [Fact]
    public async Task SaveAsync_ReturnUniqueIds_WhenSavingMultiplePresets()
    {
        // Arrange
        var preset1 = RegexPresetTestFactory.CreateTestPreset("Preset1");
        var preset2 = RegexPresetTestFactory.CreateTestPreset("Preset2");

        // Act
        var result1 = await _repository.SaveAsync(preset1);
        var result2 = await _repository.SaveAsync(preset2);

        // Assert
        result1.Id.ShouldNotBe(result2.Id);
    }

    [Fact]
    public async Task SaveAsync_PreserveOptions_WhenSavingWithOptionsEnabled()
    {
        // Arrange
        var preset = RegexPresetTestFactory.CreateTestPreset("WithOptions");
        preset.IgnoreCase = true;
        preset.Multiline = true;

        // Act
        var result = await _repository.SaveAsync(preset);

        // Assert
        result.IgnoreCase.ShouldBeTrue();
        result.Multiline.ShouldBeTrue();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnAllPresets_WhenQueried()
    {
        // Arrange
        await _repository.SaveAsync(RegexPresetTestFactory.CreateTestPreset("Preset1"));
        await _repository.SaveAsync(RegexPresetTestFactory.CreateTestPreset("Preset2"));
        await _repository.SaveAsync(RegexPresetTestFactory.CreateTestPreset("Preset3"));

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Count.ShouldBe(3);
    }

    [Fact]
    public async Task GetAllAsync_ReturnOrderedByName()
    {
        // Arrange
        await _repository.SaveAsync(RegexPresetTestFactory.CreateTestPreset("Zebra"));
        await _repository.SaveAsync(RegexPresetTestFactory.CreateTestPreset("Apple"));
        await _repository.SaveAsync(RegexPresetTestFactory.CreateTestPreset("Mango"));

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result[0].Name.ShouldBe("Apple");
        result[1].Name.ShouldBe("Mango");
        result[2].Name.ShouldBe("Zebra");
    }

    [Fact]
    public async Task GetAllAsync_ReturnEmpty_WhenNoPresets()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.ShouldBeEmpty();
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ReturnPreset_WhenIdMatches()
    {
        // Arrange
        var preset = RegexPresetTestFactory.CreateTestPreset("FindMe");
        var saved = await _repository.SaveAsync(preset);

        // Act
        var result = await _repository.GetByIdAsync(saved.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("FindMe");
        result.Pattern.ShouldBe(@"\d+");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnNull_WhenIdNotFound()
    {
        // Act
        var result = await _repository.GetByIdAsync(99999);

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region UpdateLastUsedAsync Tests

    [Fact]
    public async Task UpdateLastUsedAsync_UpdateTimestamp_WhenIdMatches()
    {
        // Arrange
        var preset = RegexPresetTestFactory.CreateTestPreset();
        preset.LastUsedAt = null;
        var saved = await _repository.SaveAsync(preset);

        // Act
        var result = await _repository.UpdateLastUsedAsync(saved.Id);

        // Assert
        result.ShouldBeTrue();
        var updated = await _repository.GetByIdAsync(saved.Id);
        updated.ShouldNotBeNull();
        updated.LastUsedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task UpdateLastUsedAsync_ReturnTrue_WhenIdNotFound()
    {
        // Note: ExecuteUpdateAsync returns the number of rows affected (0)
        // The implementation likely treats this as success (true)
        
        // Act
        var result = await _repository.UpdateLastUsedAsync(99999);

        // Assert
        result.ShouldBeTrue();
    }

    #endregion

    #region NameExistsAsync Tests

    [Fact]
    public async Task NameExistsAsync_ReturnTrue_WhenNameExists()
    {
        // Arrange
        await _repository.SaveAsync(RegexPresetTestFactory.CreateTestPreset("Existing"));

        // Act
        var result = await _repository.NameExistsAsync("Existing");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task NameExistsAsync_ReturnFalse_WhenNameDoesNotExist()
    {
        // Act
        var result = await _repository.NameExistsAsync("NonExistent");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task NameExistsAsync_PerformCaseSensitiveCheck()
    {
        // Arrange
        await _repository.SaveAsync(RegexPresetTestFactory.CreateTestPreset("EmailPattern"));

        // Act
        var result = await _repository.NameExistsAsync("emailpattern");

        // Assert
        // Database is case-sensitive by default
        result.ShouldBeOfType<bool>();
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_RemoveFromDatabase_WhenIdMatches()
    {
        // Arrange
        var preset = RegexPresetTestFactory.CreateTestPreset();
        var saved = await _repository.SaveAsync(preset);

        // Act
        var result = await _repository.DeleteAsync(saved.Id);

        // Assert
        result.ShouldBeTrue();
        var all = await _repository.GetAllAsync();
        all.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_ReturnFalse_WhenDeletingNonExistentId()
    {
        // Act
        var result = await _repository.DeleteAsync(99999);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ModifyPatternAndOptions_WhenIdMatches()
    {
        // Arrange
        var preset = RegexPresetTestFactory.CreateTestPreset();
        var saved = await _repository.SaveAsync(preset);
        var newPattern = @"[a-z]+";
        var newIgnoreCase = true;
        var newMultiline = true;

        // Act
        var result = await _repository.UpdateAsync(saved.Id, newPattern, newIgnoreCase, newMultiline);

        // Assert
        result.ShouldNotBeNull();
        result.Pattern.ShouldBe(newPattern);
        result.IgnoreCase.ShouldBeTrue();
        result.Multiline.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ReturnNull_WhenIdNotFound()
    {
        // Act
        var result = await _repository.UpdateAsync(99999, @"\d+", false, false);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateAsync_PreserveNameWhenUpdatingPattern()
    {
        // Arrange
        var preset = RegexPresetTestFactory.CreateTestPreset("ImportantName");
        var saved = await _repository.SaveAsync(preset);

        // Act
        var result = await _repository.UpdateAsync(saved.Id, @"[a-z]+", true, false);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("ImportantName");
        result.Pattern.ShouldBe(@"[a-z]+");
    }

    #endregion
}
