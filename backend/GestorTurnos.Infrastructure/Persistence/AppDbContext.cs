using GestorTurnos.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestorTurnos.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Shift> Shifts => Set<Shift>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>(e =>
        {
            e.ToTable("Employees");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.Email).HasMaxLength(254).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.CountryCode).HasMaxLength(2).IsRequired();
        });

        modelBuilder.Entity<Shift>(s =>
        {
            s.ToTable("Shifts");
            s.HasKey(x => x.Id);
            s.Property(x => x.Date).HasColumnType("date");
            s.Property(x => x.StartTime).HasColumnType("time(0)");
            s.Property(x => x.EndTime).HasColumnType("time(0)");
            s.HasIndex(x => new { x.EmployeeId, x.Date });
            s.HasOne(x => x.Employee).WithMany(x => x.Shifts)
                .HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
