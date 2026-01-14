using Wpf_Task3.Models;

namespace Wpf_Task3.Services;

/// <summary>
/// Defines data access contract for managing Record entities
/// </summary>
public interface IDataService
{
    // Retrieves all entries from the persistent storage asynchronously
    Task<List<Record>> GetAllAsync();

    // Performs a filtered search based on provided criteria
    Task<List<Record>> FilterAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
        string firstName,
        string lastName,
        string surName,
        string city,
        string country);

    // Persists a collection of records with progress tracking support
    Task AddRangeAsync(IEnumerable<Record> records, IProgress<int>? progress = null);

    // Wipes all data from the records storage
    Task ClearAsync();
}