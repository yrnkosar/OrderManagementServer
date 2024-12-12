using Microsoft.AspNetCore.Mvc;
using OrderManagement.Models;
using OrderManagement.Data;
using System.Linq;

namespace OrderManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly OrderManagementContext _context;

        public ProductController(OrderManagementContext context)
        {
            _context = context;
        }

        // Get all products
        [HttpGet]
        public ActionResult<IEnumerable<Product>> GetAllProducts()
        {
            return Ok(_context.Products.ToList());
        }

        // Add new product
        [HttpPost]
        public ActionResult<Product> AddProduct([FromBody] Product product)
        {
            _context.Products.Add(product);
            _context.SaveChanges();

            return CreatedAtAction(nameof(GetAllProducts), new { id = product.ProductId }, product);
        }

        // Update product stock
        [HttpPut("{id}")]
        public ActionResult<Product> UpdateProductStock(int id, [FromBody] Product product)
        {
            var existingProduct = _context.Products.Find(id);
            if (existingProduct == null)
                return NotFound();

            existingProduct.Stock = product.Stock;
            _context.SaveChanges();

            return Ok(existingProduct);
        }
    }
}
