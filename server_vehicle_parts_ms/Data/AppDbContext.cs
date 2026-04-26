using Microsoft.EntityFrameworkCore;
using server_vehicle_parts_ms.Data.Entities;

namespace server_vehicle_parts_ms.Data;

public class AppDbContext: DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options): base(options){}

    public DbSet<Users> Users { get; set; }
    public DbSet<Vendors> Vendors { get; set; }
    public DbSet<PartCategories> PartCategories { get; set; }
    public DbSet<VehicleParts> VehicleParts { get; set; }
    public DbSet<PurchaseInvoices> PurchaseInvoices { get; set; }
    public DbSet<PurchaseInvoiceItems> PurchaseInvoiceItems { get; set; }
    public DbSet<SalesInvoices> SalesInvoices { get; set; }
    public DbSet<SalesInvoiceLines> SalesInvoiceLines { get; set; }
    public DbSet<StockMovements> StockMovements { get; set; }
    public DbSet<Vehicles> Vehicles { get; set; }
    public DbSet<Appointments> Appointments { get; set; }
    public DbSet<PartRequests> PartRequests { get; set; }
    public DbSet<Reviews> Reviews { get; set; }
    public DbSet<Notifications> Notifications { get; set; }

    public override int SaveChanges()
    {
        StampTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void StampTimestamps()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<IHasTimestamps>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = now;
        }
    }

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

        builder.Entity<PartCategories>()
            .Property(c => c.VehicleType)
            .HasConversion<string>();
        builder.Entity<PartCategories>()
            .HasOne(c => c.Parent)
            .WithMany()
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<VehicleParts>()
            .HasIndex(p => p.Sku)
            .IsUnique();
        builder.Entity<VehicleParts>()
            .HasOne(p => p.Category)
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PurchaseInvoices>()
            .HasIndex(i => i.InvoiceNumber)
            .IsUnique();
        builder.Entity<PurchaseInvoices>()
            .Property(i => i.Status)
            .HasConversion<string>();
        builder.Entity<PurchaseInvoices>()
            .HasOne(i => i.Vendor)
            .WithMany()
            .HasForeignKey(i => i.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PurchaseInvoiceItems>()
            .HasOne(li => li.PurchaseInvoice)
            .WithMany(i => i.Items)
            .HasForeignKey(li => li.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<PurchaseInvoiceItems>()
            .HasOne(li => li.VehiclePart)
            .WithMany()
            .HasForeignKey(li => li.VehiclePartId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<SalesInvoices>()
            .HasIndex(i => i.InvoiceNumber)
            .IsUnique();
        builder.Entity<SalesInvoices>()
            .Property(i => i.Status)
            .HasConversion<string>();
        builder.Entity<SalesInvoices>()
            .HasOne(i => i.Customer)
            .WithMany()
            .HasForeignKey(i => i.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SalesInvoices>()
            .HasOne(i => i.Vehicle)
            .WithMany()
            .HasForeignKey(i => i.VehicleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<SalesInvoiceLines>()
            .HasOne(l => l.SalesInvoice)
            .WithMany(i => i.Lines)
            .HasForeignKey(l => l.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<SalesInvoiceLines>()
            .HasOne(l => l.Part)
            .WithMany()
            .HasForeignKey(l => l.PartId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StockMovements>()
            .Property(m => m.Reason)
            .HasConversion<string>();
        builder.Entity<StockMovements>()
            .HasOne(m => m.Part)
            .WithMany()
            .HasForeignKey(m => m.PartId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StockMovements>()
            .HasIndex(m => new { m.PartId, m.OccurredAt });

        builder.Entity<Vehicles>()
            .HasIndex(v => v.VehicleNumber)
            .IsUnique();
        builder.Entity<Vehicles>()
            .Property(v => v.Type)
            .HasConversion<string>();
        builder.Entity<Vehicles>()
            .HasOne(v => v.Customer)
            .WithMany()
            .HasForeignKey(v => v.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Appointments>()
            .Property(a => a.Status)
            .HasConversion<string>();
        builder.Entity<Appointments>()
            .HasOne(a => a.Customer)
            .WithMany()
            .HasForeignKey(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Appointments>()
            .HasOne(a => a.Vehicle)
            .WithMany()
            .HasForeignKey(a => a.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Appointments>()
            .HasOne(a => a.AssignedStaff)
            .WithMany()
            .HasForeignKey(a => a.AssignedStaffUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<PartRequests>()
            .Property(p => p.Status)
            .HasConversion<string>();
        builder.Entity<PartRequests>()
            .HasOne(p => p.Customer)
            .WithMany()
            .HasForeignKey(p => p.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PartRequests>()
            .HasOne(p => p.Vehicle)
            .WithMany()
            .HasForeignKey(p => p.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PartRequests>()
            .HasOne(p => p.HandledBy)
            .WithMany()
            .HasForeignKey(p => p.HandledByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Reviews>()
            .HasOne(r => r.Customer)
            .WithMany()
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Reviews>()
            .HasOne(r => r.Appointment)
            .WithMany()
            .HasForeignKey(r => r.AppointmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Notifications>()
            .Property(n => n.Type)
            .HasConversion<string>();
        builder.Entity<Notifications>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Notifications>()
            .HasIndex(n => new { n.UserId, n.IsRead });
    }
}
