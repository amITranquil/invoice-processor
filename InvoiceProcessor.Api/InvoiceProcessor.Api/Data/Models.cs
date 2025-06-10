using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvoiceProcessor.Api.Data.Models
{
    public class Invoice
    {
        public int Id { get; set; }

        [Required]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public InvoiceType Type { get; set; }

        public DateTime ProcessedDate { get; set; } = DateTime.Now;
        public DateTime? InvoiceDate { get; set; }

        public string? InvoiceNumber { get; set; }
        public string? SupplierName { get; set; }
        public string? CustomerName { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? VatAmount { get; set; }

        public ProcessingStatus Status { get; set; } = ProcessingStatus.Processing;
        public int ConfidenceScore { get; set; } // 0-100

        public string? ErrorMessage { get; set; }
        public string? RawText { get; set; }

        // Navigation
        public virtual ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    }

    public class InvoiceItem
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }

        [Required]
        public string ProductName { get; set; } = string.Empty;

        public string? ProductCode { get; set; }
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal Quantity { get; set; }

        [Required]
        public string Unit { get; set; } = "adet";

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? VatRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountAmount { get; set; }

        public int ConfidenceScore { get; set; } // 0-100

        // Navigation
        public virtual Invoice Invoice { get; set; } = null!;
        public virtual Product? Product { get; set; }
    }

    public class Product
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Code { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }

        [Required]
        public string DefaultUnit { get; set; } = "adet";

        [Column(TypeName = "decimal(18,4)")]
        public decimal CurrentStock { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        public decimal MinimumStock { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? LastPurchasePrice { get; set; }

        public DateTime? LastUpdated { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation
        public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    }

    public class StockMovement
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int? InvoiceId { get; set; }

        public MovementType Type { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal PreviousStock { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal NewStock { get; set; }

        public DateTime MovementDate { get; set; } = DateTime.Now;
        public string? Description { get; set; }

        // Navigation
        public virtual Product Product { get; set; } = null!;
        public virtual Invoice? Invoice { get; set; }
    }

    public class ProcessingLog
    {
        public int Id { get; set; }
        public int? InvoiceId { get; set; }

        public LogLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        // Navigation
        public virtual Invoice? Invoice { get; set; }
    }

    // Enums
    public enum InvoiceType
    {
        Purchase = 1,
        Sale = 2,
        PurchaseReturn = 3,
        SaleReturn = 4
    }

    public enum ProcessingStatus
    {
        Processing = 1,
        Completed = 2,
        Failed = 3,
        PendingReview = 4,
        Approved = 5
    }

    public enum MovementType
    {
        Purchase = 1,
        Sale = 2,
        PurchaseReturn = 3,
        SaleReturn = 4,
        Adjustment = 5
    }

    public enum LogLevel
    {
        Info = 1,
        Warning = 2,
        Error = 3
    }
}