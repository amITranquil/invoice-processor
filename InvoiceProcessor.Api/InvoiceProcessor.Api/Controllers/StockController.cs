using InvoiceProcessor.Api.Data;
using InvoiceProcessor.Api.Data.Models;
using InvoiceProcessor.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvoiceProcessor.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StockController : ControllerBase
    {
        private readonly InvoiceDbContext _context;
        private readonly IStockService _stockService;

        public StockController(InvoiceDbContext context, IStockService stockService)
        {
            _context = context;
            _stockService = stockService;
        }

        [HttpGet("movements")]
        public async Task<ActionResult<List<StockMovement>>> GetMovements([FromQuery] int? productId)
        {
            return await _stockService.GetMovementsAsync(productId);
        }

        [HttpGet("summary")]
        public async Task<ActionResult<object>> GetSummary()
        {
            var totalProducts = await _context.Products.CountAsync();
            var lowStockCount = await _context.Products.CountAsync(p => p.CurrentStock <= p.MinimumStock);
            var totalValue = await _context.Products
                .Where(p => p.LastPurchasePrice.HasValue)
                .SumAsync(p => p.CurrentStock * (p.LastPurchasePrice ?? 0));

            return new
            {
                TotalProducts = totalProducts,
                LowStockCount = lowStockCount,
                TotalValue = totalValue
            };
        }
    }
}