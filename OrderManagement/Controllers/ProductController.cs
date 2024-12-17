using Microsoft.AspNetCore.Mvc;
using OrderManagement.Models;

using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Data;
using Microsoft.AspNetCore.Authorization;

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
            var products = _context.Products.ToList(); // Ürünler listeleniyor
            return Ok(products);
        }

        // Get a specific product by ID
        [HttpGet("{id}")]
        public ActionResult<Product> GetProductById(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.ProductId == id);
            if (product == null)
                return NotFound();

            return Ok(product);
        }
        // Admin işlemleri için Authorization ekliyoruz.
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public ActionResult<Product> AddProduct([FromBody] Product product)
        {
            if (product == null)
            {
                return BadRequest("Product is null");
            }

            _context.Products.Add(product);
            _context.SaveChanges();

            return CreatedAtAction(nameof(GetAllProducts), new { id = product.ProductId }, product);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public ActionResult<Product> UpdateProductStock(int id, [FromBody] Product product)
        {
            var existingProduct = _context.Products.Find(id);
            if (existingProduct == null)
                return NotFound();

            existingProduct.Stock = product.Stock;
            _context.SaveChanges();

            return Ok(existingProduct);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public ActionResult DeleteProduct(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null)
                return NotFound();

            _context.Products.Remove(product);
            _context.SaveChanges();

            return NoContent();
        }

        // Get orders for a specific product
        [HttpGet("{id}/orders")]
        public ActionResult<IEnumerable<Order>> GetProductOrders(int id)
        {
            var orders = _context.Orders.Where(o => o.ProductId == id).ToList(); // İlgili ürünle yapılmış siparişler
            if (orders == null || orders.Count == 0)
                return NotFound();

            return Ok(orders);
        }

        // Add new order for a specific product
        [HttpPost("{id}/orders")]
        public ActionResult<Order> AddOrder(int id, [FromBody] Order order)
        {
            var product = _context.Products.FirstOrDefault(p => p.ProductId == id);
            if (product == null)
                return NotFound();

            // Set the product for the order
            order.ProductId = product.ProductId;
            order.TotalPrice = order.Quantity * product.Price;

            _context.Orders.Add(order);
            _context.SaveChanges();

            return CreatedAtAction(nameof(GetProductOrders), new { id = product.ProductId }, order);
        }
    }
}
