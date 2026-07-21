using Microsoft.EntityFrameworkCore;
using TmsApi.Entities;
using TmsApi.Persistence;

namespace TmsApi.Data;

public class TmsDbContext(DbContextOptions<TmsDbContext> options) : DbContext(options)
{
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<Certificate> Certificates => Set<Certificate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TmsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    
    public override int SaveChanges()
    {
        SetLastUpdated();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        SetLastUpdated();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void SetLastUpdated()
    {
        foreach (var entry in ChangeTracker.Entries<Student>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Property("LastUpdated").CurrentValue = DateTime.UtcNow;
            }
        }
    }
}