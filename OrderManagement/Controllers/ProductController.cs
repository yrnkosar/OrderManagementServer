using Microsoft.AspNetCore.Mvc;
using OrderManagement.Models;
using OrderManagement.Services;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrderManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        // Get all products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetAllProducts()
        {
            var products = await _productService.GetAllProductsAsync();
            return Ok(products);
        }

        // Get a specific product by ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProductById(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
                return NotFound("Ürün bulunamadı.");

            return Ok(product);
        }

        // Admin işlemleri için Authorization ekliyoruz.
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Product>> AddProduct([FromBody] Product product)
        {
            if (product == null)
            {
                return BadRequest("Product is null");
            }

            await _productService.CreateProductAsync(product);
            return CreatedAtAction(nameof(GetProductById), new { id = product.ProductId }, product);
        }

        [HttpPut("{id}/update-stock")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProductStock(int id, [FromBody] int newStock)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
                return NotFound("Ürün bulunamadı.");

            product.Stock = newStock;
            await _productService.UpdateProductAsync(product);

            return Ok(product);
        }
        [HttpPatch("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProductPartial(int id, [FromBody] Product updatedFields)
        {
            var existingProduct = await _productService.GetProductByIdAsync(id);
            if (existingProduct == null)
                return NotFound("Ürün bulunamadı.");

            // Sadece gönderilen alanları güncelle
            if (!string.IsNullOrEmpty(updatedFields.ProductName))
                existingProduct.ProductName = updatedFields.ProductName;

            if (updatedFields.Stock >= 0) // Stok negatif olamaz
                existingProduct.Stock = updatedFields.Stock;

            if (updatedFields.Price > 0) // Fiyat sıfırdan büyük olmalı
                existingProduct.Price = updatedFields.Price;

            if (!string.IsNullOrEmpty(updatedFields.Description))
                existingProduct.Description = updatedFields.Description;

            if (!string.IsNullOrEmpty(updatedFields.Photo))
                existingProduct.Photo = updatedFields.Photo;

            await _productService.UpdateProductAsync(existingProduct);
            return Ok(existingProduct);
        }


        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
                return NotFound();

            await _productService.DeleteProductAsync(id);
            return NoContent();
        }
    }
}
