using Microsoft.EntityFrameworkCore;
using OpenTelemetrySample.Data.Entities;

namespace OpenTelemetrySample.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Note> Notes => Set<Note>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var note = modelBuilder.Entity<Note>(e =>
        {
            e.ToTable("Notes");
            e.HasKey(n => n.Id);

            e.Property(n => n.Id).ValueGeneratedOnAdd();
            e.Property(n => n.Text).IsRequired().HasMaxLength(100);
        });
    }
}