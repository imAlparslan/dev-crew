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

    #region SaveAsync Full Field Coverage Tests

    [Fact]
    public async Task SaveAsync_PersistAllFields_WhenTemplateIsComplete()
    {
        // Arrange
        var template = new JwtBuilderTemplate
        {
            TemplateName = "CompleteTemplate",
            Algorithm = "HS512",
            Secret = "super-secret-key",
            Issuer = "my-issuer",
            Audience = "my-audience",
            Subject = "my-subject",
            ExpirationMinutes = 120,
            IncludeExpiration = true,
            CustomClaimsJson = "{\"role\":\"admin\",\"department\":\"IT\"}",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _repository.SaveAsync(template);

        // Assert
        result.Secret.ShouldBe("super-secret-key");
        result.Issuer.ShouldBe("my-issuer");
        result.Audience.ShouldBe("my-audience");
        result.Subject.ShouldBe("my-subject");
        result.ExpirationMinutes.ShouldBe(120);
        result.IncludeExpiration.ShouldBeTrue();
        result.CustomClaimsJson.ShouldBe("{\"role\":\"admin\",\"department\":\"IT\"}");
    }

    #endregion

    #region UpdateAsync Partial Field Update Tests

    [Fact]
    public async Task UpdateAsync_UpdateSingleField_WhenOnlyOneFieldChanges()
    {
        // Arrange
        var template = CreateTestTemplate("Original");
        var saved = await _repository.SaveAsync(template);
        var originalIssuer = saved.Issuer;
        saved.Audience = "new-audience";

        // Act
        var result = await _repository.UpdateAsync(saved);

        // Assert
        result.ShouldBeTrue();
        var updated = await _repository.GetByIdAsync(saved.Id);
        updated.ShouldNotBeNull();
        updated.Audience.ShouldBe("new-audience");
        updated.Issuer.ShouldBe(originalIssuer); // Other fields should remain unchanged
    }

    [Fact]
    public async Task UpdateAsync_UpdateCustomClaimsJson_WhenClaimsChange()
    {
        // Arrange
        var template = CreateTestTemplate("WithClaims");
        template.CustomClaimsJson = "{\"oldClaim\":\"oldValue\"}";
        var saved = await _repository.SaveAsync(template);
        saved.CustomClaimsJson = "{\"newClaim\":\"newValue\",\"role\":\"admin\"}";

        // Act
        var result = await _repository.UpdateAsync(saved);

        // Assert
        result.ShouldBeTrue();
        var updated = await _repository.GetByIdAsync(saved.Id);
        updated.ShouldNotBeNull();
        updated.CustomClaimsJson.ShouldBe("{\"newClaim\":\"newValue\",\"role\":\"admin\"}");
    }

    [Fact]
    public async Task UpdateAsync_PreserveCreatedAt_WhenUpdatingTemplate()
    {
        // Arrange
        var template = CreateTestTemplate("PreserveTimestamp");
        var saved = await _repository.SaveAsync(template);
        var originalCreatedAt = saved.CreatedAt;
        saved.Issuer = "updated-issuer";
        System.Threading.Thread.Sleep(10);

        // Act
        var result = await _repository.UpdateAsync(saved);

        // Assert
        result.ShouldBeTrue();
        var updated = await _repository.GetByIdAsync(saved.Id);
        updated.ShouldNotBeNull();
        updated.CreatedAt.ShouldBe(originalCreatedAt); // CreatedAt should not change
    }

    [Fact]
    public async Task UpdateAsync_PreserveId_WhenUpdatingTemplate()
    {
        // Arrange
        var template = CreateTestTemplate("PreserveId");
        var saved = await _repository.SaveAsync(template);
        var originalId = saved.Id;
        saved.Algorithm = "RS256";
        saved.Issuer = "new-issuer";

        // Act
        var result = await _repository.UpdateAsync(saved);

        // Assert
        result.ShouldBeTrue();
        var updated = await _repository.GetByIdAsync(originalId);
        updated.ShouldNotBeNull();
        updated.Id.ShouldBe(originalId);
    }

    [Fact]
    public async Task UpdateAsync_UpdateMultipleFields_WhenBatchEditOccurs()
    {
        // Arrange
        var template = CreateTestTemplate("Batch");
        var saved = await _repository.SaveAsync(template);
        saved.Algorithm = "RS512";
        saved.Issuer = "batch-issuer";
        saved.Audience = "batch-audience";
        saved.ExpirationMinutes = 240;

        // Act
        var result = await _repository.UpdateAsync(saved);

        // Assert
        result.ShouldBeTrue();
        var updated = await _repository.GetByIdAsync(saved.Id);
        updated.ShouldNotBeNull();
        updated.Algorithm.ShouldBe("RS512");
        updated.Issuer.ShouldBe("batch-issuer");
        updated.Audience.ShouldBe("batch-audience");
        updated.ExpirationMinutes.ShouldBe(240);
    }

    [Fact]
    public async Task UpdateAsync_UpdateExpirationSettings_WhenIncludeExpirationToggled()
    {
        // Arrange
        var template = CreateTestTemplate("Expiration");
        template.IncludeExpiration = true;
        template.ExpirationMinutes = 60;
        var saved = await _repository.SaveAsync(template);
        saved.IncludeExpiration = false;
        saved.ExpirationMinutes = 0;

        // Act
        var result = await _repository.UpdateAsync(saved);

        // Assert
        result.ShouldBeTrue();
        var updated = await _repository.GetByIdAsync(saved.Id);
        updated.ShouldNotBeNull();
        updated.IncludeExpiration.ShouldBeFalse();
        updated.ExpirationMinutes.ShouldBe(0);
    }

    #endregion
}
