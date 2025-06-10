using InvoiceProcessor.Api.Data;
using InvoiceProcessor.Api.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvoiceProcessor.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly InvoiceDbContext _context;

        public DashboardController(InvoiceDbContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetStats()
        {
            var totalInvoices = await _context.Invoices.CountAsync();
            var pendingInvoices = await _context.Invoices.CountAsync(i => i.Status == ProcessingStatus.PendingReview);
            var todayInvoices = await _context.Invoices.CountAsync(i => i.ProcessedDate.Date == DateTime.Today);
            var totalAmount = await _context.Invoices
                .Where(i => i.Status == ProcessingStatus.Approved)
                .SumAsync(i => i.TotalAmount);

            return new
            {
                TotalInvoices = totalInvoices,
                PendingInvoices = pendingInvoices,
                TodayInvoices = todayInvoices,
                TotalAmount = totalAmount
            };
        }
    }
}