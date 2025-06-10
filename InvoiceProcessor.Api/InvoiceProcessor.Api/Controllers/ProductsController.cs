using InvoiceProcessor.Api.Data;
using InvoiceProcessor.Api.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvoiceProcessor.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly InvoiceDbContext _context;

        public ProductsController(InvoiceDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<Product>>> GetProducts()
        {
            return await _context.Products
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            return product;
        }

        [HttpPost]
        public async Task<ActionResult<Product>> CreateProduct(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Product>> UpdateProduct(int id, Product product)
        {
            if (id != product.Id) return BadRequest();

            _context.Entry(product).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return product;
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("cleanup")]
        public async Task<IActionResult> CleanupInvalidProducts()
        {
            // Remove products with invalid names (too long, containing too much junk data)
            var invalidProducts = await _context.Products
                .Where(p => p.Name.Length > 100 ||
                           p.Name.Contains("TL") ||
                           p.Name.Contains("%") ||
                           p.Name.Contains("ETTN:") ||
                           p.Name.Contains("Tel :") ||
                           p.Name.Contains("Ödenecek Tutar") ||
                           p.Name.Contains("Not:") ||
                           p.Name.Contains("BANK") ||
                           p.Name.StartsWith("1203/") ||
                           p.Name.StartsWith("TCKN:") ||
                           System.Text.RegularExpressions.Regex.IsMatch(p.Name, @"^[\d\s.,%-]+$"))
                .ToListAsync();

            _context.Products.RemoveRange(invalidProducts);
            await _context.SaveChangesAsync();

            return Ok(new { RemovedCount = invalidProducts.Count, Message = "Invalid products cleaned up" });
        }
    }
}