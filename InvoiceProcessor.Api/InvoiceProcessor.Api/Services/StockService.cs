using InvoiceProcessor.Api.Data;
using InvoiceProcessor.Api.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

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
            try
            {
                Console.WriteLine($"[DEBUG] Updating stock for invoice {invoice.Id} with {invoice.Items.Count} items");

                foreach (var item in invoice.Items)
                {
                    try
                    {
                        var cleanedProductName = CleanProductName(item.ProductName);
                        if (!IsValidProductName(cleanedProductName))
                        {
                            Console.WriteLine($"[DEBUG] Skipping invalid product: '{item.ProductName}'");
                            continue;
                        }

                        // Use the unit from the invoice item, not from product name detection
                        var product = await GetOrCreateProductAsync(cleanedProductName, item.ProductCode, item.Unit);
                        var previousStock = product.CurrentStock;
                        var newStock = CalculateNewStock(product.CurrentStock, item.Quantity, invoice.Type);

                        product.CurrentStock = newStock;
                        product.LastUpdated = DateTime.Now;
                        
                        // Update default unit if it's more specific than current
                        if (!string.IsNullOrEmpty(item.Unit) && item.Unit != "adet")
                        {
                            product.DefaultUnit = item.Unit;
                        }

                        // Set last purchase price only for purchase invoices
                        if (invoice.Type == InvoiceType.Purchase && item.UnitPrice > 0)
                        {
                            product.LastPurchasePrice = item.UnitPrice;
                        }

                        Console.WriteLine($"[DEBUG] Stock '{product.Name}': {previousStock} -> {newStock} {product.DefaultUnit} (Type: {invoice.Type})");

                        var movement = new StockMovement
                        {
                            ProductId = product.Id,
                            InvoiceId = invoice.Id,
                            Type = MapInvoiceTypeToMovementType(invoice.Type),
                            Quantity = item.Quantity,
                            PreviousStock = previousStock,
                            NewStock = newStock,
                            Description = $"{invoice.Type} - {invoice.FileName}"
                        };

                        _context.StockMovements.Add(movement);
                    }
                    catch (Exception itemEx)
                    {
                        Console.WriteLine($"[ERROR] Failed to process item '{item.ProductName}': {itemEx.Message}");
                        continue;
                    }
                }

                await _context.SaveChangesAsync();
                Console.WriteLine($"[DEBUG] Stock update completed for invoice {invoice.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Stock update failed: {ex.Message}");
                throw;
            }
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

        public async Task<Product> GetOrCreateProductAsync(string productName, string? productCode = null, string? unit = null)
        {
            try
            {
                productName = CleanProductName(productName);
                
                if (!IsValidProductName(productName))
                {
                    throw new ArgumentException($"Invalid product name: '{productName}'");
                }

                Product? product = null;

                // 1. Search by exact name (case insensitive) - FIRST PRIORITY
                product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Name.ToLower() == productName.ToLower());

                if (product != null)
                {
                    Console.WriteLine($"[DEBUG] Found existing product by name: '{productName}' (ID: {product.Id})");
                    return product;
                }

                // 2. Search by code if provided
                if (!string.IsNullOrEmpty(productCode?.Trim()))
                {
                    product = await _context.Products
                        .FirstOrDefaultAsync(p => p.Code == productCode.Trim());
                    
                    if (product != null)
                    {
                        Console.WriteLine($"[DEBUG] Found existing product by code: '{productCode}' -> '{product.Name}' (ID: {product.Id})");
                        // Update name if found by code but name is different (merge scenario)
                        if (product.Name.ToLower() != productName.ToLower())
                        {
                            Console.WriteLine($"[DEBUG] Updating product name from '{product.Name}' to '{productName}'");
                            product.Name = productName;
                            product.LastUpdated = DateTime.Now;
                        }
                        return product;
                    }
                }

                // 3. Check for similar names (fuzzy matching) to prevent near-duplicates
                var existingProducts = await _context.Products.ToListAsync();
                foreach (var existingProduct in existingProducts)
                {
                    var similarity = CalculateSimilarity(productName, existingProduct.Name);
                    if (similarity > 0.85) // 85% similarity threshold
                    {
                        Console.WriteLine($"[DEBUG] Found similar product: '{existingProduct.Name}' (similarity: {similarity:P0}) - using existing");
                        return existingProduct;
                    }
                }

                // 4. Create new product if no match found
                Console.WriteLine($"[DEBUG] Creating new product: '{productName}'");
                
                product = new Product
                {
                    Name = productName,
                    Code = !string.IsNullOrEmpty(productCode) ? productCode.Trim() : null,
                    DefaultUnit = !string.IsNullOrEmpty(unit) ? unit : "adet",
                    CurrentStock = 0,
                    MinimumStock = 1,
                    CreatedDate = DateTime.Now
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                Console.WriteLine($"[DEBUG] Created product ID: {product.Id}");

                return product;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GetOrCreateProduct failed for '{productName}': {ex.Message}");
                throw;
            }
        }

        private bool IsValidProductName(string productName)
        {
            if (string.IsNullOrWhiteSpace(productName))
                return false;

            // Minimum length
            if (productName.Length < 3)
                return false;

            // Maximum length
            if (productName.Length > 200)
                return false;

            // Only numbers and symbols
            if (Regex.IsMatch(productName, @"^[\d\s.,%-]+$"))
                return false;

            // Must contain at least one letter
            if (!Regex.IsMatch(productName, @"[a-zA-ZğüşıöçĞÜŞİÖÇ]"))
                return false;

            // Banned words/patterns
            var bannedWords = new[] {
                "toplam", "kdv", "vat", "iskonto", "tutar", "total", "fatura",
                "invoice", "tarih", "date", "tel", "telefon", "phone", "email",
                "www", "http", "iban", "bank", "adres", "address", "vergi",
                "tax", "ödenecek", "not:", "yalnız", "platform", "hesap",
                "ltd", "a.ş", "limited", "şirket", "anonim", "mersis", "ettn:",
                "tckn:", "ara toplam", "subtotal", "hesaplanan"
            };

            var lowerName = productName.ToLower();
            if (bannedWords.Any(word => lowerName.Contains(word)))
                return false;

            // IBAN pattern check
            if (Regex.IsMatch(productName, @"TR\d{2}"))
                return false;

            return true;
        }

        private string CleanProductName(string productName)
        {
            if (string.IsNullOrWhiteSpace(productName))
                return string.Empty;

            // Trim and normalize whitespace
            productName = Regex.Replace(productName.Trim(), @"\s+", " ");

            // Remove common prefixes/suffixes
            productName = Regex.Replace(productName, @"^\d+\s*[-.]?\s*", ""); // Remove leading numbers
            productName = Regex.Replace(productName, @"\s*tl\s*$", "", RegexOptions.IgnoreCase); // Remove trailing TL
            productName = Regex.Replace(productName, @"\s+%\d+[.,]\d+.*$", ""); // Remove VAT info
            
            return productName.Trim();
        }

        private string? DetectUnit(string productName)
        {
            // DON'T detect units from product name - this causes false positives
            // Product names like "040 LT DENGE TANKLI" or "1.5M POMPA" 
            // contain measurement specs, not actual units
            
            // Always return default unit - actual unit comes from invoice parsing
            return "adet";
        }

        private double CalculateSimilarity(string text1, string text2)
        {
            if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
                return 0;

            // Normalize texts for comparison
            text1 = text1.ToLower().Trim();
            text2 = text2.ToLower().Trim();

            if (text1 == text2) return 1.0;

            // Use Levenshtein distance for similarity calculation
            var distance = LevenshteinDistance(text1, text2);
            var maxLength = Math.Max(text1.Length, text2.Length);
            return maxLength == 0 ? 1.0 : 1.0 - (double)distance / maxLength;
        }

        private int LevenshteinDistance(string s1, string s2)
        {
            var matrix = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++)
                matrix[i, 0] = i;

            for (int j = 0; j <= s2.Length; j++)
                matrix[0, j] = j;

            for (int i = 1; i <= s1.Length; i++)
            {
                for (int j = 1; j <= s2.Length; j++)
                {
                    var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }
            }

            return matrix[s1.Length, s2.Length];
        }

        private decimal CalculateNewStock(decimal currentStock, decimal quantity, InvoiceType invoiceType)
        {
            return invoiceType switch
            {
                InvoiceType.Purchase => currentStock + quantity,      // Alış: stok artar (+)
                InvoiceType.Sale => currentStock - quantity,          // Satış: stok azalır (-)
                InvoiceType.PurchaseReturn => currentStock - quantity, // Alış iadesi: stok azalır (-)
                InvoiceType.SaleReturn => currentStock + quantity,     // Satış iadesi: stok artar (+)
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