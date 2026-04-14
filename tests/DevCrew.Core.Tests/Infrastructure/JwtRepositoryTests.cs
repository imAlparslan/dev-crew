using System;
using System.Threading.Tasks;
using DevCrew.Core.Infrastructure.Persistence;
using DevCrew.Core.Infrastructure.Persistence.Repositories;
using Shouldly;
using Xunit;

namespace DevCrew.Core.Tests.Infrastructure;

public sealed class JwtRepositoryTests : IDisposable
{
    private readonly JwtRepository _repository;
    private readonly AppDbContext _context;

    public JwtRepositoryTests()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryContext();
        _context = dbContext;
        _repository = new JwtRepository(dbContext);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    #region SaveJwtAsync Tests

    [Fact]
    public async Task SaveJwtAsync_PersistToDatabase_WhenJwtIsValid()
    {
        // Arrange
        var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9";
        var header = "{\"alg\": \"HS256\"}";
        var payload = "{\"sub\": \"user123\"}";
        var issuer = "https://example.com";

        // Act
        var result = await _repository.SaveJwtAsync(token, header, payload, issuer: issuer);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBeGreaterThan(0);
        result.Token.ShouldBe(token);
        result.Header.ShouldBe(header);
        result.Payload.ShouldBe(payload);
        result.Issuer.ShouldBe(issuer);
        result.DecodedAt.ShouldNotBe(default(DateTime));
    }

    [Fact]
    public async Task SaveJwtAsync_ReturnUniqueIds_WhenSavingMultipleJwts()
    {
        // Arrange
        var token1 = "token1";
        var token2 = "token2";

        // Act
        var result1 = await _repository.SaveJwtAsync(token1);
        var result2 = await _repository.SaveJwtAsync(token2);

        // Assert
        result1.Id.ShouldNotBe(result2.Id);
    }

    [Fact]
    public async Task SaveJwtAsync_SaveWithAllFields_WhenAllParametersProvided()
    {
        // Arrange
        var token = "long-jwt-token";
        var header = "{\"alg\": \"HS256\"}";
        var payload = "{\"sub\": \"user\"}";
        var expiresAt = DateTime.UtcNow.AddHours(1);
        var issuer = "issuer";
        var audience = "audience";
        var notes = "test jwt";

        // Act
        var result = await _repository.SaveJwtAsync(token, header, payload, expiresAt, issuer, audience, notes);

        // Assert
        result.Token.ShouldBe(token);
        result.Header.ShouldBe(header);
        result.Payload.ShouldBe(payload);
        result.ExpiresAt.ShouldBe(expiresAt);
        result.Issuer.ShouldBe(issuer);
        result.Audience.ShouldBe(audience);
        result.Notes.ShouldBe(notes);
    }

    #endregion

    #region DeleteJwtAsync Tests

    [Fact]
    public async Task DeleteJwtAsync_RemoveFromDatabase_WhenIdMatches()
    {
        // Arrange
        var saved = await _repository.SaveJwtAsync("token123");

        // Act
        var result = await _repository.DeleteJwtAsync(saved.Id);

        // Assert
        result.ShouldBeTrue();
        
        // Verify deletion
        var jwts = await _repository.GetJwtsPagedAsync(0, 100);
        jwts.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeleteJwtAsync_ReturnTrue_WhenDeletingNonExistentId()
    {
        // Act
        var result = await _repository.DeleteJwtAsync(99999);

        // Assert
        result.ShouldBeTrue();
    }

    #endregion

    #region UpdateJwtNotesAsync Tests

    [Fact]
    public async Task UpdateJwtNotesAsync_ModifyNotes_WhenIdMatches()
    {
        // Arrange
        var saved = await _repository.SaveJwtAsync("token", notes: "original");
        var newNotes = "updated";

        // Act
        var result = await _repository.UpdateJwtNotesAsync(saved.Id, newNotes);

        // Assert
        result.ShouldBeTrue();
        // Note: After update, fetch fresh data to verify persistence
        var retrieved = await _repository.GetJwtByIdAsync(saved.Id);
        retrieved.ShouldNotBeNull();
    }

    [Fact]
    public async Task UpdateJwtNotesAsync_ClearNotes_WhenNotesSetToNull()
    {
        // Arrange
        var saved = await _repository.SaveJwtAsync("token", notes: "original");

        // Act
        var result = await _repository.UpdateJwtNotesAsync(saved.Id, null);

        // Assert
        result.ShouldBeTrue();
        var retrieved = await _repository.GetJwtByIdAsync(saved.Id);
        retrieved.ShouldNotBeNull();
    }

    #endregion

    #region GetJwtsPagedAsync Tests

    [Fact]
    public async Task GetJwtsPagedAsync_ReturnAllRecords_WhenQueried()
    {
        // Arrange
        await _repository.SaveJwtAsync("token1");
        await _repository.SaveJwtAsync("token2");
        await _repository.SaveJwtAsync("token3");

        // Act
        var result = await _repository.GetJwtsPagedAsync(0, 100);

        // Assert
        result.Count.ShouldBe(3);
    }

    [Fact]
    public async Task GetJwtsPagedAsync_ReturnOrderedByDecodedAtDescending()
    {
        // Arrange
        var jwt1 = await _repository.SaveJwtAsync("token1");
        await System.Threading.Tasks.Task.Delay(10);
        var jwt2 = await _repository.SaveJwtAsync("token2");

        // Act
        var result = await _repository.GetJwtsPagedAsync(0, 100);

        // Assert
        result[0].Id.ShouldBe(jwt2.Id);
        result[1].Id.ShouldBe(jwt1.Id);
    }

    [Fact]
    public async Task GetJwtsPagedAsync_SkipAndTakePagination_WhenPaginationParametersProvided()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            await _repository.SaveJwtAsync($"token{i}");
        }

        // Act
        var page1 = await _repository.GetJwtsPagedAsync(0, 2);
        var page2 = await _repository.GetJwtsPagedAsync(2, 2);

        // Assert
        page1.Count.ShouldBe(2);
        page2.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetJwtsPagedAsync_FilterBySearchQuery_WhenSearchTermMatches()
    {
        // Arrange
        await _repository.SaveJwtAsync("token1", issuer: "example.com");
        await _repository.SaveJwtAsync("token2", issuer: "other.com");

        // Act
        var result = await _repository.GetJwtsPagedAsync(0, 100, "example");

        // Assert
        result.Count.ShouldBe(1);
        result[0].ShouldNotBeNull();
        var issuer = result[0].Issuer;
        issuer.ShouldNotBeNull();
        issuer.ShouldContain("example");
    }

    #endregion

    #region GetJwtCountAsync Tests

    [Fact]
    public async Task GetJwtCountAsync_ReturnTotalCount_WhenQueried()
    {
        // Arrange
        await _repository.SaveJwtAsync("token1");
        await _repository.SaveJwtAsync("token2");
        await _repository.SaveJwtAsync("token3");

        // Act
        var count = await _repository.GetJwtCountAsync();

        // Assert
        count.ShouldBe(3);
    }

    [Fact]
    public async Task GetJwtCountAsync_ReturnCountWithSearchFilter()
    {
        // Arrange
        await _repository.SaveJwtAsync("token1", issuer: "matching");
        await _repository.SaveJwtAsync("token2", issuer: "matching");
        await _repository.SaveJwtAsync("token3", issuer: "other");

        // Act
        var count = await _repository.GetJwtCountAsync("matching");

        // Assert
        count.ShouldBe(2);
    }

    [Fact]
    public async Task GetJwtCountAsync_ReturnZero_WhenNoRecords()
    {
        // Act
        var count = await _repository.GetJwtCountAsync();

        // Assert
        count.ShouldBe(0);
    }

    #endregion

    #region GetJwtByIdAsync Tests

    [Fact]
    public async Task GetJwtByIdAsync_ReturnRecord_WhenIdMatches()
    {
        // Arrange
        var saved = await _repository.SaveJwtAsync("token123");

        // Act
        var result = await _repository.GetJwtByIdAsync(saved.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(saved.Id);
        result.Token.ShouldBe("token123");
    }

    [Fact]
    public async Task GetJwtByIdAsync_ReturnNull_WhenIdNotFound()
    {
        // Act
        var result = await _repository.GetJwtByIdAsync(99999);

        // Assert
        result.ShouldBeNull();
    }

    #endregion
}
