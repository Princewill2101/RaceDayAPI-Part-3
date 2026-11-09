using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;

namespace RaceDayAPI.Tests;

// Creates a fresh in-memory database for every test.
// Each test gets its own isolated database so they can't affect each other.
public static class TestDbHelper
{
    public static AppDbContext GetInMemoryDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var db = new AppDbContext(options);
        db.Database.EnsureCreated();   // runs OnModelCreating (seeds EventTypes)
        return db;
    }
}
