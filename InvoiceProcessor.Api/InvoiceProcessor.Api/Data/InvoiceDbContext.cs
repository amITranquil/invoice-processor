using InvoiceProcessor.Api.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceProcessor.Api.Data
{
    public class InvoiceDbContext : DbContext
    {
        public InvoiceDbContext(DbContextOptions<InvoiceDbContext> options) : base(options) { }

        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<ProcessingLog> ProcessingLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Invoice Configuration
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FileName).HasMaxLength(500);
                entity.Property(e => e.InvoiceNumber).HasMaxLength(100);
                entity.Property(e => e.SupplierName).HasMaxLength(500);
                entity.Property(e => e.CustomerName).HasMaxLength(500);
                entity.HasIndex(e => e.InvoiceNumber);
                entity.HasIndex(e => e.ProcessedDate);
            });

            // InvoiceItem Configuration
            modelBuilder.Entity<InvoiceItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ProductName).HasMaxLength(500);
                entity.Property(e => e.ProductCode).HasMaxLength(100);
                entity.Property(e => e.Unit).HasMaxLength(50);

                entity.HasOne(e => e.Invoice)
                      .WithMany(e => e.Items)
                      .HasForeignKey(e => e.InvoiceId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Product Configuration
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(500);
                entity.Property(e => e.Code).HasMaxLength(100);
                entity.Property(e => e.Category).HasMaxLength(200);
                entity.Property(e => e.DefaultUnit).HasMaxLength(50);

                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.Name);
            });

            // StockMovement Configuration
            modelBuilder.Entity<StockMovement>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Description).HasMaxLength(1000);

                entity.HasOne(e => e.Product)
                      .WithMany(e => e.StockMovements)
                      .HasForeignKey(e => e.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Invoice)
                      .WithMany()
                      .HasForeignKey(e => e.InvoiceId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.MovementDate);
            });

            // ProcessingLog Configuration
            modelBuilder.Entity<ProcessingLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Message).HasMaxLength(1000);

                entity.HasOne(e => e.Invoice)
                      .WithMany()
                      .HasForeignKey(e => e.InvoiceId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.Timestamp);
            });
        }
    }
}