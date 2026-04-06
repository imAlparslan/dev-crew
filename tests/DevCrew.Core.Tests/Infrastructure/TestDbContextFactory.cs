using System;
using System.IO;
using System.Linq;
using DevCrew.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevCrew.Core.Tests.Infrastructure;

/// <summary>
/// Factory for creating isolated test database contexts
/// </summary>
public static class TestDbContextFactory
{
    /// <summary>
    /// Creates an isolated AppDbContext for testing using file-based SQLite databases.
    /// Each call creates a new independent context with isolated data in a temporary file-based database.
    /// SQLite is used instead of EF Core's in-memory provider because it supports
    /// ExecuteDelete and ExecuteUpdate operations used by the repositories.
    /// File-based databases are used instead of in-memory to ensure isolation between tests.
    /// </summary>
    /// <returns>A new AppDbContext configured for isolated SQLite testing</returns>
    public static AppDbContext CreateInMemoryContext()
    {
        // Use a temporary file-based database to ensure each context has isolated data
        var dbPath = Path.Combine(Path.GetTempPath(), $"devcrew_test_{Guid.NewGuid()}.db");
        var connectionString = $"Data Source={dbPath};";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connectionString)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        
        // Store the path for cleanup if needed
        context.Database.GetDbConnection().ConnectionString = connectionString;
        
        return context;
    }

    /// <summary>
    /// Creates multiple isolated in-memory contexts for parallel testing scenarios
    /// </summary>
    /// <param name="count">Number of contexts to create</param>
    /// <returns>Array of independent AppDbContext instances</returns>
    public static AppDbContext[] CreateMultipleInMemoryContexts(int count)
    {
        return Enumerable.Range(0, count)
            .Select(_ => CreateInMemoryContext())
            .ToArray();
    }
}

