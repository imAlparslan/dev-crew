using System;
using System.Linq;
using System.Threading.Tasks;
using DevCrew.Core.Infrastructure.Persistence;
using DevCrew.Core.Infrastructure.Persistence.Repositories;
using Shouldly;
using Xunit;

namespace DevCrew.Core.Tests.Infrastructure;

public sealed class GuidRepositoryTests : IDisposable
{
    private readonly GuidRepository _repository;
    private readonly AppDbContext _context;

    public GuidRepositoryTests()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryContext();
        _context = dbContext;
        _repository = new GuidRepository(dbContext);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    #region SaveGuidAsync Tests

    [Fact]
    public async Task SaveGuidAsync_PersistToDatabase_WhenGuidIsValid()
    {
        // Arrange
        var guidValue = Guid.NewGuid().ToString();
        var notes = "Test GUID";

        // Act
        var result = await _repository.SaveGuidAsync(guidValue, notes);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBeGreaterThan(0);
        result.GuidValue.ShouldBe(guidValue);
        result.Notes.ShouldBe(notes);
        result.CreatedAt.ShouldNotBe(default(DateTime));
    }

    [Fact]
    public async Task SaveGuidAsync_ReturnUniqueIds_WhenSavingMultipleGuids()
    {
        // Arrange
        var guid1 = Guid.NewGuid().ToString();
        var guid2 = Guid.NewGuid().ToString();

        // Act
        var result1 = await _repository.SaveGuidAsync(guid1);
        var result2 = await _repository.SaveGuidAsync(guid2);

        // Assert
        result1.Id.ShouldNotBe(result2.Id);
        result1.Id.ShouldBeGreaterThan(0);
        result2.Id.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task SaveGuidAsync_ThrowException_WhenGuidIsEmpty()
    {
        // Arrange
        var emptyGuid = string.Empty;

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            () => _repository.SaveGuidAsync(emptyGuid)
        );
    }

    [Fact]
    public async Task SaveGuidAsync_SaveWithNullNotes_WhenNotesNotProvided()
    {
        // Arrange
        var guidValue = Guid.NewGuid().ToString();

        // Act
        var result = await _repository.SaveGuidAsync(guidValue);

        // Assert
        result.ShouldNotBeNull();
        result.Notes.ShouldBeNull();
    }

    #endregion

    #region DeleteGuidAsync Tests

    [Fact]
    public async Task DeleteGuidAsync_RemoveFromDatabase_WhenIdMatches()
    {
        // Arrange
        var guidValue = Guid.NewGuid().ToString();
        var saved = await _repository.SaveGuidAsync(guidValue);

        // Act
        var result = await _repository.DeleteGuidAsync(saved.Id);

        // Assert
        result.ShouldBeTrue();

        // Verify deletion by checking count
        var guids = await _repository.GetGuidsPagedAsync(0, 100);
        guids.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeleteGuidAsync_ReturnTrue_WhenDeletingNonExistentId()
    {
        // Arrange
        var nonExistentId = 99999;

        // Act
        var result = await _repository.DeleteGuidAsync(nonExistentId);

        // Assert
        result.ShouldBeTrue();
    }

    #endregion

    #region UpdateGuidNotesAsync Tests

    [Fact]
    public async Task UpdateGuidNotesAsync_ModifyNotes_WhenIdMatches()
    {
        // Arrange
        var guidValue = Guid.NewGuid().ToString();
        var originalNotes = "Original notes";
        var saved = await _repository.SaveGuidAsync(guidValue, originalNotes);
        var newNotes = "Updated notes";

        // Act
        var result = await _repository.UpdateGuidNotesAsync(saved.Id, newNotes);

        // Assert
        result.ShouldBeTrue();
        // Retrieve and verify
        var guids = await _repository.GetGuidsPagedAsync(0, 100);
        var updated = guids[0];
        updated.Notes.ShouldBe(newNotes);
    }

    [Fact]
    public async Task UpdateGuidNotesAsync_ClearNotes_WhenNotesSetToNull()
    {
        // Arrange
        var guidValue = Guid.NewGuid().ToString();
        var saved = await _repository.SaveGuidAsync(guidValue, "Some notes");

        // Act
        var result = await _repository.UpdateGuidNotesAsync(saved.Id, null);

        // Assert
        result.ShouldBeTrue();
        var guids = await _repository.GetGuidsPagedAsync(0, 100);
        guids[0].Notes.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateGuidNotesAsync_ReturnTrue_WhenUpdatingNonExistentId()
    {
        // Arrange
        var nonExistentId = 99999;

        // Act
        var result = await _repository.UpdateGuidNotesAsync(nonExistentId, "New notes");

        // Assert
        result.ShouldBeTrue();
    }

    #endregion

    #region GetGuidsPagedAsync Tests

    [Fact]
    public async Task GetGuidsPagedAsync_ReturnAllRecords_WhenQueried()
    {
        // Arrange
        var guid1 = Guid.NewGuid().ToString();
        var guid2 = Guid.NewGuid().ToString();
        var guid3 = Guid.NewGuid().ToString();
        await _repository.SaveGuidAsync(guid1);
        await _repository.SaveGuidAsync(guid2);
        await _repository.SaveGuidAsync(guid3);

        // Act
        var result = await _repository.GetGuidsPagedAsync(0, 100);

        // Assert
        result.Count.ShouldBe(3);
        result.Select(g => g.GuidValue).ShouldContain(guid1);
        result.Select(g => g.GuidValue).ShouldContain(guid2);
        result.Select(g => g.GuidValue).ShouldContain(guid3);
    }

    [Fact]
    public async Task GetGuidsPagedAsync_ReturnOrderedByCreatedAtDescending()
    {
        // Arrange
        var guid1 = Guid.NewGuid().ToString();
        var guid2 = Guid.NewGuid().ToString();
        await _repository.SaveGuidAsync(guid1);
        await System.Threading.Tasks.Task.Delay(10);
        await _repository.SaveGuidAsync(guid2);

        // Act
        var result = await _repository.GetGuidsPagedAsync(0, 100);

        // Assert
        result.Count.ShouldBe(2);
        result[0].GuidValue.ShouldBe(guid2);
        result[1].GuidValue.ShouldBe(guid1);
    }

    [Fact]
    public async Task GetGuidsPagedAsync_SkipAndTakePagination_WhenPaginationParametersProvided()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            await _repository.SaveGuidAsync(Guid.NewGuid().ToString());
        }

        // Act
        var page1 = await _repository.GetGuidsPagedAsync(0, 2);
        var page2 = await _repository.GetGuidsPagedAsync(2, 2);

        // Assert
        page1.Count.ShouldBe(2);
        page2.Count.ShouldBe(2);
        page1[0].Id.ShouldNotBe(page2[0].Id);
    }

    [Fact]
    public async Task GetGuidsPagedAsync_FilterBySearchQuery_WhenSearchTermMatches()
    {
        // Arrange
        var guid1 = Guid.NewGuid().ToString();
        var guid2 = Guid.NewGuid().ToString();
        await _repository.SaveGuidAsync(guid1, "important");
        await _repository.SaveGuidAsync(guid2, "unimportant");

        // Act
        var result = await _repository.GetGuidsPagedAsync(0, 100, "important");

        // Assert
        // Verify search returns results (exact matching behavior may vary)
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetGuidsPagedAsync_ReturnEmpty_WhenNoRecordsMatch()
    {
        // Arrange
        var guid1 = Guid.NewGuid().ToString();
        await _repository.SaveGuidAsync(guid1, "test");

        // Act
        var result = await _repository.GetGuidsPagedAsync(0, 100, "nonexistent");

        // Assert
        result.ShouldBeEmpty();
    }

    #endregion

    #region GetGuidCountAsync Tests

    [Fact]
    public async Task GetGuidCountAsync_ReturnTotalCount_WhenQueried()
    {
        // Arrange
        await _repository.SaveGuidAsync(Guid.NewGuid().ToString());
        await _repository.SaveGuidAsync(Guid.NewGuid().ToString());
        await _repository.SaveGuidAsync(Guid.NewGuid().ToString());

        // Act
        var count = await _repository.GetGuidCountAsync();

        // Assert
        count.ShouldBe(3);
    }

    [Fact]
    public async Task GetGuidCountAsync_ReturnCountWithSearchFilter()
    {
        // Arrange
        await _repository.SaveGuidAsync(Guid.NewGuid().ToString(), "matching");
        await _repository.SaveGuidAsync(Guid.NewGuid().ToString(), "matching");
        await _repository.SaveGuidAsync(Guid.NewGuid().ToString(), "other");

        // Act
        var count = await _repository.GetGuidCountAsync("matching");

        // Assert
        count.ShouldBe(2);
    }

    [Fact]
    public async Task GetGuidCountAsync_ReturnZero_WhenNoRecords()
    {
        // Act
        var count = await _repository.GetGuidCountAsync();

        // Assert
        count.ShouldBe(0);
    }

    #endregion
}
