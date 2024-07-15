using InventoryManagementAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace InventoryManagementAPI.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor httpContextAccessor)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<InventoryItem> InventoryItems { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<Tenant> Tenants { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Seed tenants
            builder.Entity<Tenant>().HasData(
                new Tenant { Id = "tenant1", Name = "ABC Company" },
                new Tenant { Id = "tenant2", Name = "XYZ Company" }
            );

            // Seed Warehouses
            builder.Entity<Warehouse>().HasData(
                new Warehouse { Id = 1, Name = "Warehouse A", Location = "Location A", TenantId = "tenant1" },
                new Warehouse { Id = 2, Name = "Warehouse B", Location = "Location B", TenantId = "tenant2" }
            );

            // Seed InventoryItems
            builder.Entity<InventoryItem>().HasData(
                new InventoryItem { Id = 1, Name = "Item 1", Description = "Description 1", Price = 10, Quantity = 100, Category = "Category A", TenantId = "tenant1" },
                new InventoryItem { Id = 2, Name = "Item 2", Description = "Description 2", Price = 20, Quantity = 50, Category = "Category B", TenantId = "tenant1" },
                new InventoryItem { Id = 3, Name = "Item 3", Description = "Description 3", Price = 15, Quantity = 75, Category = "Category A", TenantId = "tenant1" },
                new InventoryItem { Id = 4, Name = "Item 4", Description = "Description 4", Price = 50, Quantity = 200, Category = "Category C", TenantId = "tenant2" },
                new InventoryItem { Id = 5, Name = "Item 5", Description = "Description 5", Price = 68, Quantity = 80, Category = "Category A", TenantId = "tenant2" },
                new InventoryItem { Id = 6, Name = "Item 6", Description = "Description 6", Price = 100, Quantity = 95, Category = "Category B", TenantId = "tenant2" }
            );

            // Many-to-many relationship between Warehouse and InventoryItem
            builder.Entity<Warehouse>()
                .HasMany(w => w.InventoryItems)
                .WithMany(i => i.Warehouses)
                .UsingEntity<Dictionary<string, object>>(
                    "WarehouseInventoryItem",
                    j => j.HasOne<InventoryItem>().WithMany().HasForeignKey("InventoryItemId"),
                    j => j.HasOne<Warehouse>().WithMany().HasForeignKey("WarehouseId"),
                    j =>
                    {
                        j.HasKey("WarehouseId", "InventoryItemId");
                        j.ToTable("WarehouseInventoryItems");
                    });

            // One-to-many relationship between Tenant and ApplicationUser
            builder.Entity<Tenant>()
                .HasMany(t => t.Users)
                .WithOne(u => u.Tenant)
                .HasForeignKey(u => u.TenantId)
                .IsRequired();

            // Comment out global query filters for debugging
            // builder.Entity<InventoryItem>().HasQueryFilter(i => i.TenantId == GetTenantId());
            // builder.Entity<Warehouse>().HasQueryFilter(w => w.TenantId == GetTenantId());
        }

        private static void SeedRoles(ModelBuilder builder)
        {
            builder.Entity<IdentityRole>().HasData(
                new IdentityRole() { Name = "Admin", ConcurrencyStamp = "1", NormalizedName = "ADMIN" },
                new IdentityRole() { Name = "Manager", ConcurrencyStamp = "2", NormalizedName = "MANAGER" }
                );
        }

        private string GetTenantId()
        {
            return _httpContextAccessor.HttpContext?.Items["TenantId"] as string ?? "default_tenant";
        }

        public override int SaveChanges()
        {
            ApplyTenantId();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyTenantId();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyTenantId()
        {
            var tenantId = GetTenantId();
            foreach (var entry in ChangeTracker.Entries<EntityBase>().Where(e => e.State == EntityState.Added))
            {
                entry.Entity.TenantId = tenantId;
            }
        }
    }
}
