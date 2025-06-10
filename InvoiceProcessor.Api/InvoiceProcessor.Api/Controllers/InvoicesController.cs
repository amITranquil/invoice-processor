using InvoiceProcessor.Api.Data;
using InvoiceProcessor.Api.Data.Models;
using InvoiceProcessor.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvoiceProcessor.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoicesController : ControllerBase
    {
        private readonly InvoiceDbContext _context;
        private readonly IFileProcessingService _fileProcessingService;
        private readonly IStockService _stockService;

        public InvoicesController(
            InvoiceDbContext context,
            IFileProcessingService fileProcessingService,
            IStockService stockService)
        {
            _context = context;
            _fileProcessingService = fileProcessingService;
            _stockService = stockService;
        }

        [HttpGet]
        public async Task<ActionResult<List<Invoice>>> GetInvoices()
        {
            return await _context.Invoices
                .Include(i => i.Items)
                .OrderByDescending(i => i.ProcessedDate)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Invoice>> GetInvoice(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null) return NotFound();
            return invoice;
        }

        [HttpPost("upload")]
        public async Task<ActionResult<Invoice>> UploadInvoice(IFormFile file, [FromForm] string? invoiceType = null)
        {
            try
            {
                Console.WriteLine($"[DEBUG] Upload request received:");
                Console.WriteLine($"[DEBUG] - File: {file?.FileName}");
                Console.WriteLine($"[DEBUG] - File size: {file?.Length} bytes");
                Console.WriteLine($"[DEBUG] - InvoiceType: '{invoiceType}'");

                if (file == null || file.Length == 0)
                    return BadRequest("Dosya geçerli değil");

                if (!_fileProcessingService.IsValidFileType(file.FileName))
                    return BadRequest("Geçersiz dosya türü");

                string? normalizedInvoiceType = null;
                if (!string.IsNullOrEmpty(invoiceType))
                {
                    normalizedInvoiceType = invoiceType.ToLower().Trim();
                }

                using var stream = file.OpenReadStream();
                var invoice = await _fileProcessingService.ProcessFileAsync(stream, file.FileName, normalizedInvoiceType);

                if (invoice.Items == null || !invoice.Items.Any())
                {
                    Console.WriteLine("[WARNING] No valid items extracted from invoice");
                }

                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();

                return Ok(invoice);
            }
            catch (ArgumentException argEx)
            {
                Console.WriteLine($"[ERROR] Validation error: {argEx.Message}");
                return BadRequest($"Geçersiz veri: {argEx.Message}");
            }
            catch (InvalidOperationException opEx)
            {
                Console.WriteLine($"[ERROR] Operation error: {opEx.Message}");
                return BadRequest($"İşlem hatası: {opEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Upload failed: {ex.Message}");
                Console.WriteLine($"[ERROR] Inner exception: {ex.InnerException?.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { 
                    error = "Fatura işleme hatası", 
                    message = ex.Message,
                    details = ex.InnerException?.Message 
                });
            }
        }

        [HttpPost("{id}/approve")]
        public async Task<ActionResult<Invoice>> ApproveInvoice(int id)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.Items)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (invoice == null) 
                    return NotFound("Fatura bulunamadı");

                Console.WriteLine($"[DEBUG] Approving invoice {id}, type: {invoice.Type}");

                invoice.Status = ProcessingStatus.Approved;
                
                await _stockService.UpdateStockAsync(invoice);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[DEBUG] Invoice {id} approved and stock updated");

                return Ok(invoice);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Approval failed: {ex.Message}");
                return StatusCode(500, $"Onay işlemi hatası: {ex.Message}");
            }
        }

        [HttpPut("{id}/update-and-approve")]
        public async Task<ActionResult<Invoice>> UpdateAndApproveInvoice(int id, [FromBody] Invoice updatedInvoice)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.Items)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (invoice == null) 
                    return NotFound("Fatura bulunamadı");

                // Update invoice items
                _context.InvoiceItems.RemoveRange(invoice.Items);
                foreach (var item in updatedInvoice.Items)
                {
                    item.InvoiceId = invoice.Id;
                    item.Id = 0; // Reset ID for new items
                    _context.InvoiceItems.Add(item);
                }

                invoice.TotalAmount = updatedInvoice.TotalAmount;
                invoice.Status = ProcessingStatus.Approved;

                await _context.SaveChangesAsync();

                // Update stock with new items
                await _stockService.UpdateStockAsync(invoice);
                await _context.SaveChangesAsync();

                return Ok(invoice);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Update and approval failed: {ex.Message}");
                return StatusCode(500, $"Güncelleme hatası: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInvoice(int id)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.Items)
                    .FirstOrDefaultAsync(i => i.Id == id);
                    
                if (invoice == null) 
                    return NotFound();

                Console.WriteLine($"[DEBUG] Deleting invoice {id} with {invoice.Items.Count} items");

                // Eğer fatura onaylanmışsa stok hareketlerini geri al
                if (invoice.Status == ProcessingStatus.Approved)
                {
                    await ReverseStockMovements(invoice);
                }

                // Bu faturadan oluşturulan ve sadece bu faturada bulunan ürünleri sil
                await DeleteOrphanedProducts(invoice);

                _context.Invoices.Remove(invoice);
                await _context.SaveChangesAsync();
                
                Console.WriteLine($"[DEBUG] Invoice {id} deleted successfully");
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Delete failed: {ex.Message}");
                return StatusCode(500, $"Silme işlemi hatası: {ex.Message}");
            }
        }

        private async Task ReverseStockMovements(Invoice invoice)
        {
            var movements = await _context.StockMovements
                .Where(sm => sm.InvoiceId == invoice.Id)
                .ToListAsync();

            foreach (var movement in movements)
            {
                var product = await _context.Products.FindAsync(movement.ProductId);
                if (product != null)
                {
                    product.CurrentStock = movement.PreviousStock;
                    product.LastUpdated = DateTime.Now;
                }
            }

            _context.StockMovements.RemoveRange(movements);
        }

        private async Task DeleteOrphanedProducts(Invoice invoice)
        {
            foreach (var item in invoice.Items)
            {
                // Bu ürünü başka faturalarda da kullanıp kullanmadığını kontrol et
                var otherInvoiceItems = await _context.InvoiceItems
                    .Where(ii => ii.ProductName == item.ProductName && ii.InvoiceId != invoice.Id)
                    .CountAsync();

                if (otherInvoiceItems == 0)
                {
                    // Sadece bu faturada bulunan ürünü sil
                    var productsToDelete = await _context.Products
                        .Where(p => p.Name == item.ProductName || 
                                   (!string.IsNullOrEmpty(item.ProductCode) && p.Code == item.ProductCode))
                        .ToListAsync();

                    foreach (var product in productsToDelete)
                    {
                        Console.WriteLine($"[DEBUG] Deleting orphaned product: {product.Name}");
                        _context.Products.Remove(product);
                    }
                }
            }
        }
    }
}