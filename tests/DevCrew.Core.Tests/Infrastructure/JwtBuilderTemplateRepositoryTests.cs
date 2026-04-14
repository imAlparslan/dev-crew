using System;
using System.Threading.Tasks;
using DevCrew.Core.Domain.Models;
using DevCrew.Core.Infrastructure.Persistence;
using DevCrew.Core.Infrastructure.Persistence.Repositories;
using Shouldly;
using Xunit;

namespace DevCrew.Core.Tests.Infrastructure;

public sealed class JwtBuilderTemplateRepositoryTests : IDisposable
{
    private readonly JwtBuilderTemplateRepository _repository;
    private readonly AppDbContext _context;

    public JwtBuilderTemplateRepositoryTests()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryContext();
        _context = dbContext;
        _repository = new JwtBuilderTemplateRepository(dbContext);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    private static JwtBuilderTemplate CreateTestTemplate(string name = "Test Template")
    {
        return new JwtBuilderTemplate
        {
            TemplateName = name,
            Algorithm = "HS256",
            Secret = "test-secret-key",
            Issuer = "test-issuer",
            Audience = "test-audience",
            Subject = "test-subject",
            ExpirationMinutes = 60,
            IncludeExpiration = true,
            CustomClaimsJson = "{}",
            CreatedAt = DateTime.UtcNow
        };
    }

    #region SaveAsync Tests

    [Fact]
    public async Task SaveAsync_PersistToDatabase_WhenTemplateIsValid()
    {
        // Arrange
        var template = CreateTestTemplate("MyTemplate");

        // Act
        var result = await _repository.SaveAsync(template);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBeGreaterThan(0);
        result.TemplateName.ShouldBe("MyTemplate");
        result.Algorithm.ShouldBe("HS256");
    }

    [Fact]
    public async Task SaveAsync_ReturnUniqueIds_WhenSavingMultipleTemplates()
    {
        // Arrange
        var template1 = CreateTestTemplate("Template1");
        var template2 = CreateTestTemplate("Template2");

        // Act
        var result1 = await _repository.SaveAsync(template1);
        var result2 = await _repository.SaveAsync(template2);

        // Assert
        result1.Id.ShouldNotBe(result2.Id);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ModifyTemplate_WhenIdMatches()
    {
        // Arrange
        var template = CreateTestTemplate("Original");
        var saved = await _repository.SaveAsync(template);
        saved.Issuer = "new-issuer";

        // Act
        var result = await _repository.UpdateAsync(saved);

        // Assert
        result.ShouldBeTrue();
        var updated = await _repository.GetByIdAsync(saved.Id);
        updated.ShouldNotBeNull();
        updated.Issuer.ShouldBe("new-issuer");
    }

    [Fact]
    public async Task UpdateAsync_ReturnFalse_WhenTemplateNotFound()
    {
        // Arrange
        var template = CreateTestTemplate();
        template.Id = 99999;

        // Act
        var result = await _repository.UpdateAsync(template);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_RemoveFromDatabase_WhenIdMatches()
    {
        // Arrange
        var template = CreateTestTemplate();
        var saved = await _repository.SaveAsync(template);

        // Act
        var result = await _repository.DeleteAsync(saved.Id);

        // Assert
        result.ShouldBeTrue();
        var all = await _repository.GetAllAsync();
        all.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_ReturnTrue_WhenDeletingNonExistentId()
    {
        // Act
        var result = await _repository.DeleteAsync(99999);

        // Assert
        result.ShouldBeTrue();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnAllTemplates_WhenQueried()
    {
        // Arrange
        await _repository.SaveAsync(CreateTestTemplate("Template1"));
        await _repository.SaveAsync(CreateTestTemplate("Template2"));
        await _repository.SaveAsync(CreateTestTemplate("Template3"));

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Count.ShouldBe(3);
    }

    [Fact]
    public async Task GetAllAsync_ReturnOrderedByName()
    {
        // Arrange
        await _repository.SaveAsync(CreateTestTemplate("Zebra"));
        await _repository.SaveAsync(CreateTestTemplate("Apple"));
        await _repository.SaveAsync(CreateTestTemplate("Mango"));

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result[0].TemplateName.ShouldBe("Apple");
        result[1].TemplateName.ShouldBe("Mango");
        result[2].TemplateName.ShouldBe("Zebra");
    }

    [Fact]
    public async Task GetAllAsync_ReturnEmpty_WhenNoTemplates()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.ShouldBeEmpty();
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ReturnTemplate_WhenIdMatches()
    {
        // Arrange
        var template = CreateTestTemplate("FindMe");
        var saved = await _repository.SaveAsync(template);

        // Act
        var result = await _repository.GetByIdAsync(saved.Id);

        // Assert
        result.ShouldNotBeNull();
        result.TemplateName.ShouldBe("FindMe");
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
        var template = CreateTestTemplate();
        template.LastUsedAt = null;
        var saved = await _repository.SaveAsync(template);

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

    #region TemplateNameExistsAsync Tests

    [Fact]
    public async Task TemplateNameExistsAsync_ReturnTrue_WhenNameExists()
    {
        // Arrange
        await _repository.SaveAsync(CreateTestTemplate("Existing"));

        // Act
        var result = await _repository.TemplateNameExistsAsync("Existing");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task TemplateNameExistsAsync_ReturnFalse_WhenNameDoesNotExist()
    {
        // Act
        var result = await _repository.TemplateNameExistsAsync("NonExistent");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task TemplateNameExistsAsync_ExcludeIdWhenChecking_WhenUpdatingTemplate()
    {
        // Arrange
        var template1 = CreateTestTemplate("MyTemplate");
        var saved1 = await _repository.SaveAsync(template1);
        var template2 = CreateTestTemplate("OtherTemplate");
        await _repository.SaveAsync(template2);

        // Act - Check if "MyTemplate" exists excluding the ID of the template with that name
        var result = await _repository.TemplateNameExistsAsync("MyTemplate", excludeId: saved1.Id);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task TemplateNameExistsAsync_ReturnTrueForDuplicate_WhenAnotherTemplateHasSameName()
    {
        // Arrange
        var template1 = CreateTestTemplate("Duplicate");
        _ = await _repository.SaveAsync(template1);
        var template2 = CreateTestTemplate("Duplicate");
        await _repository.SaveAsync(template2);

        // Act - Check "Duplicate" without excluding any ID
        var result = await _repository.TemplateNameExistsAsync("Duplicate");

        // Assert
        result.ShouldBeTrue();
    }

    #endregion
}
