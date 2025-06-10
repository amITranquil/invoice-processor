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
        public async Task<ActionResult<Invoice>> UploadInvoice(IFormFile file)
        {
            if (!_fileProcessingService.IsValidFileType(file.FileName))
                return BadRequest("Geçersiz dosya türü");

            using var stream = file.OpenReadStream();
            var invoice = await _fileProcessingService.ProcessFileAsync(stream, file.FileName);

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            return invoice;
        }

        [HttpPost("{id}/approve")]
        public async Task<ActionResult<Invoice>> ApproveInvoice(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null) return NotFound();

            invoice.Status = ProcessingStatus.Approved;
            await _stockService.UpdateStockAsync(invoice);
            await _context.SaveChangesAsync();

            return invoice;
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInvoice(int id)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null) return NotFound();

            _context.Invoices.Remove(invoice);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}