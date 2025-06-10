using InvoiceProcessor.Api.Data;
using InvoiceProcessor.Api.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceProcessor.Api.Services
{
    public class StockService : IStockService
    {
        private readonly InvoiceDbContext _context;

        public StockService(InvoiceDbContext context)
        {
            _context = context;
        }

        public async Task UpdateStockAsync(Invoice invoice)
        {
            foreach (var item in invoice.Items)
            {
                var product = await GetOrCreateProductAsync(item.ProductName, item.ProductCode);

                var previousStock = product.CurrentStock;
                var newStock = CalculateNewStock(product.CurrentStock, item.Quantity, invoice.Type);

                // Stok güncelle
                product.CurrentStock = newStock;
                product.LastUpdated = DateTime.Now;

                if (invoice.Type == InvoiceType.Purchase)
                {
                    product.LastPurchasePrice = item.UnitPrice;
                }

                // Stok hareketi kaydet
                var movement = new StockMovement
                {
                    ProductId = product.Id,
                    InvoiceId = invoice.Id,
                    Type = MapInvoiceTypeToMovementType(invoice.Type),
                    Quantity = item.Quantity,
                    PreviousStock = previousStock,
                    NewStock = newStock,
                    Description = $"{invoice.Type.ToString()} - {invoice.FileName}"
                };

                _context.StockMovements.Add(movement);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<StockMovement>> GetMovementsAsync(int? productId = null)
        {
            IQueryable<StockMovement> query = _context.StockMovements
                .Include(sm => sm.Product)
                .Include(sm => sm.Invoice);

            if (productId.HasValue)
            {
                query = query.Where(sm => sm.ProductId == productId.Value);
            }

            return await query.OrderByDescending(sm => sm.MovementDate).ToListAsync();
        }

        public async Task<Product> GetOrCreateProductAsync(string productName, string? productCode = null)
        {
            // Önce koda göre ara
            Product? product = null;

            if (!string.IsNullOrEmpty(productCode))
            {
                product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Code == productCode);
            }

            // Kod bulunamazsa isme göre ara (fuzzy matching)
            if (product == null)
            {
                product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Name.Contains(productName) ||
                                            productName.Contains(p.Name));
            }

            // Hiç bulunamazsa yeni ürün oluştur
            if (product == null)
            {
                product = new Product
                {
                    Name = productName,
                    Code = productCode,
                    DefaultUnit = "adet",
                    CurrentStock = 0,
                    CreatedDate = DateTime.Now
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();
            }

            return product;
        }

        private decimal CalculateNewStock(decimal currentStock, decimal quantity, InvoiceType invoiceType)
        {
            return invoiceType switch
            {
                InvoiceType.Purchase => currentStock + quantity,      // Alış: stok artar
                InvoiceType.Sale => currentStock - quantity,          // Satış: stok azalır
                InvoiceType.PurchaseReturn => currentStock - quantity, // Alış iadesi: stok azalır
                InvoiceType.SaleReturn => currentStock + quantity,     // Satış iadesi: stok artar
                _ => currentStock
            };
        }

        private MovementType MapInvoiceTypeToMovementType(InvoiceType invoiceType)
        {
            return invoiceType switch
            {
                InvoiceType.Purchase => MovementType.Purchase,
                InvoiceType.Sale => MovementType.Sale,
                InvoiceType.PurchaseReturn => MovementType.PurchaseReturn,
                InvoiceType.SaleReturn => MovementType.SaleReturn,
                _ => MovementType.Adjustment
            };
        }
    }
}