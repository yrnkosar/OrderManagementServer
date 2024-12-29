using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OrderManagement.Models;
using OrderManagement.Services;
using OrderManagement.Hubs; 
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
        private readonly IHubContext<OrderHub> _hubContext;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly SystemStatusService _systemStatusService;

        public ProductController(IProductService productService, IHubContext<OrderHub> hubContext, SystemStatusService systemStatusService)
        {
            _productService = productService;
            _hubContext = hubContext;
            _systemStatusService = systemStatusService; 
        }
        
        [HttpGet]
         public async Task<ActionResult<IEnumerable<Product>>> GetAllProducts()
         {
             var products = await _productService.GetAllProductsAsync();
             var visibleProducts = products.Where(p => p.Visibility).ToList(); 
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
                await _hubContext.Clients.All.SendAsync("ProductAdded", product); 
            });

            return CreatedAtAction(nameof(GetProductById), new { id = product.ProductId }, product);
        }

        [HttpPut("{id}/update-stock")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProductStock(int id, [FromBody] int newStock)
        {
            await _systemStatusService.SetAdminProcessing(true);
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
                return NotFound("Ürün bulunamadı.");

            await PerformAdminActionAsync(async () =>
            {
                product.Stock = newStock;
                product.Visibility = true;
                await _productService.UpdateProductAsync(product);
                await _hubContext.Clients.All.SendAsync("ProductUpdated", product.ProductId, product.Stock);
            });
            await _systemStatusService.SetAdminProcessing(false);
            return Ok(product);
        }

        [HttpPatch("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProductPartial(int id, [FromBody] Product updatedFields)
        {
            await _systemStatusService.SetAdminProcessing(true);
            await Task.Delay(10000);
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
                existingProduct.Visibility = true;
                await _productService.UpdateProductAsync(existingProduct);
                await _hubContext.Clients.All.SendAsync("ProductUpdated", existingProduct.ProductId);
            });
            await _systemStatusService.SetAdminProcessing(false);
            return Ok(existingProduct);
        }
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            await _systemStatusService.SetAdminProcessing(true);
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
                return NotFound("Ürün bulunamadı.");

            await PerformAdminActionAsync(async () =>
            {
                product.Visibility = false; 
                await _productService.UpdateProductAsync(product); 
                await _hubContext.Clients.All.SendAsync("ProductDeleted", id);
            });
            await _systemStatusService.SetAdminProcessing(false);
            return NoContent();
        }/*
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
                return NotFound("Ürün bulunamadı.");

            await PerformAdminActionAsync(async () =>
            {
                await _productService.DeleteProductAsync(id);
                await _hubContext.Clients.All.SendAsync("ProductDeleted", id);
            });

            return NoContent();
        }
        */
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
