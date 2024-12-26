/*using Microsoft.AspNetCore.Mvc;
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
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        [HttpPut("{id}/update-stock")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProductStock(int id, [FromBody] int newStock)
        {
            await _semaphore.WaitAsync(); // Kilidi al
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                    return NotFound("Ürün bulunamadı.");

                product.Stock = newStock;
                await _productService.UpdateProductAsync(product);

                return Ok(product);
            }
            finally
            {
                _semaphore.Release(); // Kilidi serbest bırak
            }
        }

        /*  [HttpPut("{id}/update-stock")]
          [Authorize(Roles = "Admin")]
          public async Task<IActionResult> UpdateProductStock(int id, [FromBody] int newStock)
          {
              var product = await _productService.GetProductByIdAsync(id);
              if (product == null)
                  return NotFound("Ürün bulunamadı.");

              product.Stock = newStock;
              await _productService.UpdateProductAsync(product);

              return Ok(product);
          }*/
/* [HttpPatch("{id}")]
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
*/using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OrderManagement.Models;
using OrderManagement.Services;
using OrderManagement.Hubs; // Bu satır eklenmeli
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrderManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IHubContext<OrderHub> _hubContext; // OrderHub kullanılıyor
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public ProductController(IProductService productService, IHubContext<OrderHub> hubContext)
        {
            _productService = productService;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetAllProducts()
        {
            var products = await _productService.GetAllProductsAsync();
            var visibleProducts = products.Where(p => p.Visibility).ToList(); // Sadece görünür ürünler
            return Ok(visibleProducts);
        }
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProductById(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
                return NotFound("Ürün bulunamadı.");

            return Ok(product);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Product>> AddProduct([FromBody] Product product)
        {
            if (product == null)
                return BadRequest("Ürün bilgisi eksik.");

            await PerformAdminActionAsync(async () =>
            {
                await _productService.CreateProductAsync(product);
                await _hubContext.Clients.All.SendAsync("ProductAdded", product); // SignalR bildirimi
            });

            return CreatedAtAction(nameof(GetProductById), new { id = product.ProductId }, product);
        }

        [HttpPut("{id}/update-stock")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProductStock(int id, [FromBody] int newStock)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
                return NotFound("Ürün bulunamadı.");

            await PerformAdminActionAsync(async () =>
            {
                product.Stock = newStock;
                await _productService.UpdateProductAsync(product);
                await _hubContext.Clients.All.SendAsync("ProductUpdated", product.ProductId, product.Stock);
            });

            return Ok(product);
        }

        [HttpPatch("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProductPartial(int id, [FromBody] Product updatedFields)
        {
            var existingProduct = await _productService.GetProductByIdAsync(id);
            if (existingProduct == null)
                return NotFound("Ürün bulunamadı.");

            await PerformAdminActionAsync(async () =>
            {
                if (!string.IsNullOrEmpty(updatedFields.ProductName))
                    existingProduct.ProductName = updatedFields.ProductName;

                if (updatedFields.Stock >= 0)
                    existingProduct.Stock = updatedFields.Stock;

                if (updatedFields.Price > 0)
                    existingProduct.Price = updatedFields.Price;

                if (!string.IsNullOrEmpty(updatedFields.Description))
                    existingProduct.Description = updatedFields.Description;

                if (!string.IsNullOrEmpty(updatedFields.Photo))
                    existingProduct.Photo = updatedFields.Photo;

                await _productService.UpdateProductAsync(existingProduct);
                await _hubContext.Clients.All.SendAsync("ProductUpdated", existingProduct.ProductId);
            });

            return Ok(existingProduct);
        }

      
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
                return NotFound("Ürün bulunamadı.");

            await PerformAdminActionAsync(async () =>
            {
                product.Visibility = false; // Ürünü görünmez yap
                await _productService.UpdateProductAsync(product); // Güncelleme işlemi
                await _hubContext.Clients.All.SendAsync("ProductDeleted", id);
            });

            return NoContent();
        }
        private async Task PerformAdminActionAsync(Func<Task> action)
        {
            await _semaphore.WaitAsync();
            try
            {
                await action();
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
