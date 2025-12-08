using Microsoft.EntityFrameworkCore;
using Wpf_Task3.Models;

namespace Wpf_Task3;

public class AppDbContext : DbContext
{
    public DbSet<Record> Records { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(
            @"Server=.\SQLEXPRESS;Database=Db_Task3;Trusted_Connection=True;TrustServerCertificate=True;");
    }
}
