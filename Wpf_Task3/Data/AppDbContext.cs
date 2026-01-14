using Microsoft.EntityFrameworkCore;
using Wpf_Task3.Models;

// EF Core database context
namespace Wpf_Task3.Data;

public class AppDbContext : DbContext
{
    // Represents the Records table in the database
    public DbSet<Record> Records => Set<Record>();

    // DbContext is configured via dependency injection
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
}
