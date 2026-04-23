using Microsoft.EntityFrameworkCore;
using ProductionControl.Models;

namespace ProductionControl.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductionLine> ProductionLines => Set<ProductionLine>();

    public DbSet<Material> Materials => Set<Material>();

    public DbSet<ProductMaterial> ProductMaterials => Set<ProductMaterial>();

    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProductMaterial>()
            .HasKey(productMaterial => new { productMaterial.ProductId, productMaterial.MaterialId });

        modelBuilder.Entity<ProductMaterial>()
            .HasOne(productMaterial => productMaterial.Product)
            .WithMany(product => product.ProductMaterials)
            .HasForeignKey(productMaterial => productMaterial.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductMaterial>()
            .HasOne(productMaterial => productMaterial.Material)
            .WithMany(material => material.ProductMaterials)
            .HasForeignKey(productMaterial => productMaterial.MaterialId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorkOrder>()
            .HasOne(workOrder => workOrder.Product)
            .WithMany(product => product.WorkOrders)
            .HasForeignKey(workOrder => workOrder.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorkOrder>()
            .HasOne(workOrder => workOrder.ProductionLine)
            .WithMany(line => line.WorkOrders)
            .HasForeignKey(workOrder => workOrder.ProductionLineId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ProductionLine>()
            .HasOne(line => line.CurrentWorkOrder)
            .WithMany()
            .HasForeignKey(line => line.CurrentWorkOrderId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Product>()
            .Property(product => product.Specifications)
            .HasColumnType("TEXT");
    }
}