using Microsoft.EntityFrameworkCore;
using server_vehicle_parts_ms.Data.Entities;

namespace server_vehicle_parts_ms.Data;

public class AppDbContext: DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options): base(options){}
    
    public DbSet<Users> Users { get; set; }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Users>()
            .Property(u => u.Role)
            .HasConversion<string>();
        builder.Entity<Users>()
            .HasIndex(u => u.Email)
            .IsUnique();
        builder.Entity<Users>()
            .HasIndex(u => u.PhoneNumber)
            .IsUnique();
    }
}