using InvoiceProcessor.Api.Data;
using InvoiceProcessor.Api.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace InvoiceProcessor.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CleanupController : ControllerBase
    {
        private readonly InvoiceDbContext _context;

        public CleanupController(InvoiceDbContext context)
        {
            _context = context;
        }

        [HttpPost("merge-duplicate-products")]
        public async Task<IActionResult> MergeDuplicateProducts()
        {
            try
            {
                var allProducts = await _context.Products.ToListAsync();
                var duplicateGroups = allProducts
                    .GroupBy(p => p.Name.ToLower().Trim())
                    .Where(g => g.Count() > 1)
                    .ToList();

                int mergedCount = 0;
                var mergedInfo = new List<object>();

                foreach (var group in duplicateGroups)
                {
                    var products = group.OrderBy(p => p.Id).ToList();
                    var keepProduct = products.First(); // Keep the first one
                    var duplicates = products.Skip(1).ToList();

                    Console.WriteLine($"[DEBUG] Merging duplicates for: {keepProduct.Name}");
                    Console.WriteLine($"[DEBUG] Keeping ID: {keepProduct.Id}, Removing IDs: {string.Join(",", duplicates.Select(d => d.Id))}");

                    // Merge stock quantities
                    decimal totalStock = products.Sum(p => p.CurrentStock);
                    keepProduct.CurrentStock = totalStock;
                    keepProduct.LastUpdated = DateTime.Now;

                    // Update product code if any duplicate has one and main doesn't
                    if (string.IsNullOrEmpty(keepProduct.Code))
                    {
                        var productWithCode = duplicates.FirstOrDefault(d => !string.IsNullOrEmpty(d.Code));
                        if (productWithCode != null)
                        {
                            keepProduct.Code = productWithCode.Code;
                        }
                    }

                    // Update last purchase price to the highest one
                    var highestPrice = products
                        .Where(p => p.LastPurchasePrice.HasValue)
                        .Max(p => p.LastPurchasePrice);
                    if (highestPrice.HasValue)
                    {
                        keepProduct.LastPurchasePrice = highestPrice;
                    }

                    // Update stock movements to point to kept product
                    foreach (var duplicate in duplicates)
                    {
                        var movements = await _context.StockMovements
                            .Where(sm => sm.ProductId == duplicate.Id)
                            .ToListAsync();

                        foreach (var movement in movements)
                        {
                            movement.ProductId = keepProduct.Id;
                        }

                        Console.WriteLine($"[DEBUG] Updated {movements.Count} stock movements for product ID {duplicate.Id}");
                    }

                    // Track merge info
                    mergedInfo.Add(new
                    {
                        KeptProduct = new { keepProduct.Id, keepProduct.Name, keepProduct.CurrentStock },
                        RemovedProducts = duplicates.Select(d => new { d.Id, d.Name, d.CurrentStock }).ToList()
                    });

                    // Remove duplicate products
                    _context.Products.RemoveRange(duplicates);
                    mergedCount += duplicates.Count;
                }

                await _context.SaveChangesAsync();

                return Ok(new { 
                    Message = "Duplicate products merged successfully",
                    MergedCount = mergedCount,
                    GroupsProcessed = duplicateGroups.Count,
                    Details = mergedInfo
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Merge failed: {ex.Message}");
                return StatusCode(500, $"Merge failed: {ex.Message}");
            }
        }

        [HttpGet("preview-duplicate-products")]
        public async Task<IActionResult> PreviewDuplicateProducts()
        {
            try
            {
                var allProducts = await _context.Products.ToListAsync();
                var duplicateGroups = allProducts
                    .GroupBy(p => p.Name.ToLower().Trim())
                    .Where(g => g.Count() > 1)
                    .Select(g => new
                    {
                        ProductName = g.Key,
                        Count = g.Count(),
                        Products = g.Select(p => new
                        {
                            p.Id,
                            p.Name,
                            p.Code,
                            p.CurrentStock,
                            p.LastPurchasePrice,
                            p.CreatedDate
                        }).OrderBy(p => p.Id).ToList(),
                        TotalStock = g.Sum(p => p.CurrentStock)
                    })
                    .OrderByDescending(g => g.Count)
                    .ToList();

                return Ok(new
                {
                    TotalDuplicateGroups = duplicateGroups.Count,
                    TotalDuplicateProducts = duplicateGroups.Sum(g => g.Count - 1), // -1 because we keep one from each group
                    Groups = duplicateGroups
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Preview failed: {ex.Message}");
            }
        }

        [HttpPost("invalid-products")]
        public async Task<IActionResult> CleanupInvalidProducts()
        {
            try
            {
                var invalidProducts = await _context.Products
                    .Where(p => 
                        // Too short or too long
                        p.Name.Length < 3 || p.Name.Length > 200 ||
                        // Only numbers/symbols
                        Regex.IsMatch(p.Name, @"^[\d\s.,%-]+$") ||
                        // Contains banned words
                        p.Name.ToLower().Contains("toplam") ||
                        p.Name.ToLower().Contains("kdv") ||
                        p.Name.ToLower().Contains("vat") ||
                        p.Name.ToLower().Contains("iskonto") ||
                        p.Name.ToLower().Contains("tutar") ||
                        p.Name.ToLower().Contains("total") ||
                        p.Name.ToLower().Contains("fatura") ||
                        p.Name.ToLower().Contains("invoice") ||
                        p.Name.ToLower().Contains("tarih") ||
                        p.Name.ToLower().Contains("tel") ||
                        p.Name.ToLower().Contains("telefon") ||
                        p.Name.ToLower().Contains("phone") ||
                        p.Name.ToLower().Contains("email") ||
                        p.Name.ToLower().Contains("www") ||
                        p.Name.ToLower().Contains("http") ||
                        p.Name.ToLower().Contains("iban") ||
                        p.Name.ToLower().Contains("bank") ||
                        p.Name.ToLower().Contains("adres") ||
                        p.Name.ToLower().Contains("address") ||
                        p.Name.ToLower().Contains("vergi") ||
                        p.Name.ToLower().Contains("tax") ||
                        p.Name.ToLower().Contains("ödenecek") ||
                        p.Name.ToLower().Contains("not:") ||
                        p.Name.ToLower().Contains("yalnız") ||
                        p.Name.ToLower().Contains("platform") ||
                        p.Name.ToLower().Contains("hesap") ||
                        p.Name.ToLower().Contains("ltd") ||
                        p.Name.ToLower().Contains("a.ş") ||
                        p.Name.ToLower().Contains("limited") ||
                        p.Name.ToLower().Contains("şirket") ||
                        p.Name.ToLower().Contains("anonim") ||
                        p.Name.ToLower().Contains("mersis") ||
                        p.Name.ToLower().Contains("ettn:") ||
                        p.Name.ToLower().Contains("tckn:") ||
                        p.Name.Contains("TR") && Regex.IsMatch(p.Name, @"TR\d{2}") ||
                        // No letters
                        !Regex.IsMatch(p.Name, @"[a-zA-ZğüşıöçĞÜŞİÖÇ]")
                    )
                    .ToListAsync();

                // First remove stock movements for these products
                foreach (var product in invalidProducts)
                {
                    var movements = await _context.StockMovements
                        .Where(sm => sm.ProductId == product.Id)
                        .ToListAsync();
                    _context.StockMovements.RemoveRange(movements);
                }

                _context.Products.RemoveRange(invalidProducts);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    Message = "Invalid products cleaned up successfully",
                    RemovedCount = invalidProducts.Count,
                    RemovedProducts = invalidProducts.Select(p => new { p.Id, p.Name, p.CurrentStock }).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Cleanup failed: {ex.Message}");
            }
        }

        [HttpGet("preview-invalid-products")]
        public async Task<IActionResult> PreviewInvalidProducts()
        {
            var invalidProducts = await _context.Products
                .Where(p => 
                    p.Name.Length < 3 || p.Name.Length > 200 ||
                    Regex.IsMatch(p.Name, @"^[\d\s.,%-]+$") ||
                    p.Name.ToLower().Contains("toplam") ||
                    p.Name.ToLower().Contains("kdv") ||
                    p.Name.ToLower().Contains("tutar") ||
                    p.Name.ToLower().Contains("fatura") ||
                    p.Name.ToLower().Contains("tel") ||
                    p.Name.ToLower().Contains("iban") ||
                    p.Name.ToLower().Contains("bank") ||
                    p.Name.ToLower().Contains("vergi") ||
                    p.Name.ToLower().Contains("not:") ||
                    p.Name.ToLower().Contains("ettn:") ||
                    p.Name.ToLower().Contains("tckn:") ||
                    !Regex.IsMatch(p.Name, @"[a-zA-ZğüşıöçĞÜŞİÖÇ]")
                )
                .Select(p => new { p.Id, p.Name, p.Code, p.CurrentStock })
                .ToListAsync();

            return Ok(new { 
                Count = invalidProducts.Count,
                Products = invalidProducts
            });
        }

        [HttpPost("invalid-invoice-items")]
        public async Task<IActionResult> CleanupInvalidInvoiceItems()
        {
            try
            {
                var invalidItems = await _context.InvoiceItems
                    .Where(ii => 
                        ii.ProductName.Length < 3 ||
                        ii.ProductName.Length > 200 ||
                        Regex.IsMatch(ii.ProductName, @"^[\d\s.,%-]+$") ||
                        ii.ProductName.ToLower().Contains("toplam") ||
                        ii.ProductName.ToLower().Contains("kdv") ||
                        ii.ProductName.ToLower().Contains("tutar") ||
                        ii.ProductName.ToLower().Contains("total") ||
                        ii.ProductName.ToLower().Contains("fatura") ||
                        ii.ProductName.ToLower().Contains("tel") ||
                        ii.ProductName.ToLower().Contains("iban") ||
                        ii.ProductName.ToLower().Contains("bank") ||
                        ii.ProductName.ToLower().Contains("vergi") ||
                        ii.ProductName.ToLower().Contains("not:") ||
                        ii.ProductName.ToLower().Contains("ettn:") ||
                        ii.ProductName.ToLower().Contains("tckn:") ||
                        !Regex.IsMatch(ii.ProductName, @"[a-zA-ZğüşıöçĞÜŞİÖÇ]")
                    )
                    .ToListAsync();

                _context.InvoiceItems.RemoveRange(invalidItems);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    Message = "Invalid invoice items cleaned up successfully",
                    RemovedCount = invalidItems.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Cleanup failed: {ex.Message}");
            }
        }

        [HttpPost("all-data")]
        public async Task<IActionResult> CleanupAllData()
        {
            try
            {
                // Remove all data in correct order (foreign key constraints)
                _context.StockMovements.RemoveRange(_context.StockMovements);
                _context.InvoiceItems.RemoveRange(_context.InvoiceItems);
                _context.Invoices.RemoveRange(_context.Invoices);
                _context.Products.RemoveRange(_context.Products);
                _context.ProcessingLogs.RemoveRange(_context.ProcessingLogs);

                await _context.SaveChangesAsync();

                return Ok(new { Message = "All data cleaned up successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Cleanup failed: {ex.Message}");
            }
        }

        [HttpPost("fix-stock-calculations")]
        public async Task<IActionResult> FixStockCalculations()
        {
            try
            {
                // Get all products
                var products = await _context.Products.ToListAsync();
                var fixedProducts = new List<object>();

                foreach (var product in products)
                {
                    // Recalculate stock from movements
                    var movements = await _context.StockMovements
                        .Where(sm => sm.ProductId == product.Id)
                        .OrderBy(sm => sm.MovementDate)
                        .ThenBy(sm => sm.Id)
                        .ToListAsync();

                    decimal calculatedStock = 0;
                    var oldStock = product.CurrentStock;

                    foreach (var movement in movements)
                    {
                        switch (movement.Type)
                        {
                            case MovementType.Purchase:
                            case MovementType.SaleReturn:
                                calculatedStock += movement.Quantity;
                                break;
                            case MovementType.Sale:
                            case MovementType.PurchaseReturn:
                                calculatedStock -= movement.Quantity;
                                break;
                        }
                    }

                    if (Math.Abs(calculatedStock - product.CurrentStock) > 0.001m)
                    {
                        Console.WriteLine($"[DEBUG] Fixing stock for {product.Name}: {product.CurrentStock} -> {calculatedStock}");
                        product.CurrentStock = calculatedStock;
                        product.LastUpdated = DateTime.Now;

                        fixedProducts.Add(new
                        {
                            product.Id,
                            product.Name,
                            OldStock = oldStock,
                            NewStock = calculatedStock,
                            Difference = calculatedStock - oldStock
                        });
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Stock calculations fixed successfully",
                    FixedProductCount = fixedProducts.Count,
                    FixedProducts = fixedProducts
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Fix stock calculations failed: {ex.Message}");
                return StatusCode(500, $"Fix failed: {ex.Message}");
            }
        }
    }
}