using System;
using System.Threading.Tasks;
using DevCrew.Core.Infrastructure.Persistence.Repositories;
using Shouldly;
using Xunit;

namespace DevCrew.Core.Tests.Infrastructure;

public sealed class AppSettingsRepositoryTests : IDisposable
{
    private readonly AppSettingsRepository _repository;
    private readonly IDisposable _context;

    public AppSettingsRepositoryTests()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryContext();
        _context = dbContext;
        _repository = new AppSettingsRepository(dbContext);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    #region GetOrCreateAsync Tests

    [Fact]
    public async Task GetOrCreateAsync_CreateNewSettings_WhenNoneExist()
    {
        // Act
        var result = await _repository.GetOrCreateAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBeGreaterThan(0);
        result.LanguageCultureName.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetOrCreateAsync_ReturnExistingSettings_WhenAlreadyCreated()
    {
        // Arrange
        var first = await _repository.GetOrCreateAsync();

        // Act
        var second = await _repository.GetOrCreateAsync();

        // Assert
        second.Id.ShouldBe(first.Id);
        second.LanguageCultureName.ShouldBe(first.LanguageCultureName);
    }

    [Fact]
    public async Task GetOrCreateAsync_ReturnSingletonRecord_WhenCalledMultipleTimes()
    {
        // Arrange
        var first = await _repository.GetOrCreateAsync();

        // Act
        var second = await _repository.GetOrCreateAsync();
        var third = await _repository.GetOrCreateAsync();

        // Assert
        first.Id.ShouldBe(second.Id);
        second.Id.ShouldBe(third.Id);
    }

    #endregion

    #region UpdateLanguageAsync Tests

    [Fact]
    public async Task UpdateLanguageAsync_ModifyLanguage_WhenSettingsExist()
    {
        // Arrange
        await _repository.GetOrCreateAsync();
        var newLanguage = "de-DE";

        // Act
        var result = await _repository.UpdateLanguageAsync(newLanguage);

        // Assert
        result.ShouldBeTrue();
        var updated = await _repository.GetOrCreateAsync();
        updated.LanguageCultureName.ShouldBe(newLanguage);
    }

    [Fact]
    public async Task UpdateLanguageAsync_CreateAndUpdate_WhenSettingsNotExist()
    {
        // Arrange
        var newLanguage = "fr-FR";

        // Act
        var result = await _repository.UpdateLanguageAsync(newLanguage);

        // Assert
        result.ShouldBeTrue();
        var settings = await _repository.GetOrCreateAsync();
        settings.LanguageCultureName.ShouldBe(newLanguage);
    }

    [Fact]
    public async Task UpdateLanguageAsync_UpdateMultipleTimes_WhenCalledSequentially()
    {
        // Arrange
        await _repository.GetOrCreateAsync();
        var language1 = "en-US";
        var language2 = "de-DE";

        // Act
        await _repository.UpdateLanguageAsync(language1);
        var result = await _repository.UpdateLanguageAsync(language2);

        // Assert
        result.ShouldBeTrue();
        var settings = await _repository.GetOrCreateAsync();
        settings.LanguageCultureName.ShouldBe(language2);
    }

    #endregion

    #region UpdateFontSettingsAsync Tests

    [Fact]
    public async Task UpdateFontSettingsAsync_ModifyFontSettings_WhenSettingsExist()
    {
        // Arrange
        await _repository.GetOrCreateAsync();
        var sizePreference = "Medium";
        var uiFont = "Segoe UI";
        var headingFont = "Arial";
        var buttonFont = "Verdana";
        var contentFont = "Calibri";

        // Act
        var result = await _repository.UpdateFontSettingsAsync(sizePreference, uiFont, headingFont, buttonFont, contentFont);

        // Assert
        result.ShouldBeTrue();
        var updated = await _repository.GetOrCreateAsync();
        updated.FontSizePreference.ShouldBe(sizePreference);
        updated.UiFontFamily.ShouldBe(uiFont);
        updated.HeadingFontFamily.ShouldBe(headingFont);
        updated.ButtonFontFamily.ShouldBe(buttonFont);
        updated.ContentFontFamily.ShouldBe(contentFont);
    }

    [Fact]
    public async Task UpdateFontSettingsAsync_CreateAndUpdate_WhenSettingsNotExist()
    {
        // Arrange
        var sizePreference = "Large";
        var uiFont = "Segoe UI";
        var headingFont = "Arial";
        var buttonFont = "Verdana";
        var contentFont = "Calibri";

        // Act
        var result = await _repository.UpdateFontSettingsAsync(sizePreference, uiFont, headingFont, buttonFont, contentFont);

        // Assert
        result.ShouldBeTrue();
        var settings = await _repository.GetOrCreateAsync();
        settings.FontSizePreference.ShouldBe(sizePreference);
    }

    [Fact]
    public async Task UpdateFontSettingsAsync_PreservePreviousLanguage_WhenUpdatingFonts()
    {
        // Arrange
        var settings = await _repository.GetOrCreateAsync();
        var originalLanguage = settings.LanguageCultureName;
        var newLanguage = "tr-TR";
        await _repository.UpdateLanguageAsync(newLanguage);

        // Act
        await _repository.UpdateFontSettingsAsync("Small", "Arial", "Arial", "Arial", "Arial");

        // Assert
        var updated = await _repository.GetOrCreateAsync();
        updated.LanguageCultureName.ShouldBe(newLanguage);
        updated.FontSizePreference.ShouldBe("Small");
    }

    #endregion

    #region Persistence Tests

    [Fact]
    public async Task Settings_AreSingleton_WhenModelingMultipleGetOrCalls()
    {
        // Arrange
        var settings1 = await _repository.GetOrCreateAsync();
        var id1 = settings1.Id;
        await _repository.UpdateLanguageAsync("es-ES");

        // Act - Get again from same repository (same context)
        var settings2 = await _repository.GetOrCreateAsync();

        // Assert
        settings2.Id.ShouldBe(id1);
        settings2.LanguageCultureName.ShouldBe("es-ES");
    }

    #endregion
}
