using Microsoft.EntityFrameworkCore;
using Wpf_Task3.Data;
using Wpf_Task3.Models;

namespace Wpf_Task3.Services;

/// <summary>
/// Data access service providing asynchronous CRUD operations for Record entities.
/// Optimized for high-performance bulk operations and UI responsiveness.
/// </summary>
public class DataService : IDataService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public DataService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <summary>
    /// Retrieves all records from the database using NoTracking for maximum performance.
    /// </summary>
    public async Task<List<Record>> GetAllAsync()
    {
        // ConfigureAwait(false) is used to prevent potential deadlocks in WPF synchronization context
        using var db = await _dbFactory.CreateDbContextAsync().ConfigureAwait(false);
        return await db.Records.AsNoTracking().ToListAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Searches records based on multiple optional filter criteria.
    /// </summary>
    public async Task<List<Record>> FilterAsync(
        DateTime? from,
        DateTime? to,
        string firstName,
        string lastName,
        string surName,
        string city,
        string country)
    {
        using var db = await _dbFactory.CreateDbContextAsync().ConfigureAwait(false);
        var query = db.Records.AsNoTracking().AsQueryable();

        // Apply date range filters if provided
        if (from.HasValue) query = query.Where(r => r.RecordDate >= from);
        if (to.HasValue) query = query.Where(r => r.RecordDate <= to);

        // Apply text-based filters using contains (SQL LIKE)
        if (!string.IsNullOrWhiteSpace(firstName)) query = query.Where(r => r.FirstName.Contains(firstName));
        if (!string.IsNullOrWhiteSpace(lastName)) query = query.Where(r => r.LastName.Contains(lastName));
        if (!string.IsNullOrWhiteSpace(surName)) query = query.Where(r => r.SurName.Contains(surName));
        if (!string.IsNullOrWhiteSpace(city)) query = query.Where(r => r.City.Contains(city));
        if (!string.IsNullOrWhiteSpace(country)) query = query.Where(r => r.Country.Contains(country));

        return await query.ToListAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Performs high-speed bulk insertion of records using batching and disabled change tracking.
    /// Reports progress back to the UI thread.
    /// </summary>
    public async Task AddRangeAsync(IEnumerable<Record> records, IProgress<int>? progress = null)
    {

        using var db = await _dbFactory.CreateDbContextAsync().ConfigureAwait(false);

        // Disable automatic change detection to significantly speed up bulk inserts
        db.ChangeTracker.AutoDetectChangesEnabled = false;

        int batchSize = 500; // Define batch size to optimize memory and SQL transaction logs
        int processed = 0;

        var batch = new List<Record>(batchSize);

        foreach (var record in records)
        {
            batch.Add(record);

            if (batch.Count == batchSize)
            {
                await db.Records.AddRangeAsync(batch).ConfigureAwait(false);
                await db.SaveChangesAsync().ConfigureAwait(false);

                processed += batch.Count;
                progress?.Report(processed);

                batch.Clear();
            }
        }

        // Save remaining records if batch is not empty
        if (batch.Count > 0)
        {
            await db.Records.AddRangeAsync(batch).ConfigureAwait(false);
            await db.SaveChangesAsync().ConfigureAwait(false);

            processed += batch.Count;
            progress?.Report(processed);
        }
    }


    /// <summary>
    /// Wipes all data from the Records table using the highly efficient TRUNCATE command.
    /// </summary>
    public async Task ClearAsync()
    {
        using var db = await _dbFactory.CreateDbContextAsync().ConfigureAwait(false);
        // Using raw SQL TRUNCATE as it is much faster than individual row deletion
        await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE Records").ConfigureAwait(false);
    }
}