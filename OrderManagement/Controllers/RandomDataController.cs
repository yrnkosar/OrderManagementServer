using Microsoft.AspNetCore.Mvc;
using OrderManagement.Models;
using OrderManagement.Repositories;
using OrderManagement.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrderManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RandomDataController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly IProductService _productService;
        private readonly ICustomerRepository _customerRepository;
        private readonly IProductRepository _productRepository;

        public RandomDataController(ICustomerService customerService, IProductService productService, ICustomerRepository customerRepository, IProductRepository productRepository)
        {
            _customerService = customerService;
            _productService = productService;
            _customerRepository = customerRepository;
            _productRepository = productRepository;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateRandomData()
        {
            Random rand = new Random();

            // Mevcut müşteri sayısını kontrol et
            var existingCustomers = await _customerRepository.GetAllAsync();
            int currentCustomerCount = existingCustomers.Count();

            // Eğer 5-10 arası müşteri yoksa, eksik olanları ekle
            int customerCount = rand.Next(5, 11); // 5 ile 10 arasında rastgele müşteri sayısı
            if (currentCustomerCount < customerCount)
            {
                // Eksik müşterileri oluştur
                for (int i = currentCustomerCount; i < customerCount; i++)
                {
                    var customer = new Customer
                    {
                        CustomerName = "Customer" + (i + 1),
                        Budget = rand.Next(500, 3001), // 500 ile 3000 arasında rastgele bütçe
                        CustomerType = (i < 2) ? "Premium" : "Standard", // İlk iki müşteri Premium
                        TotalSpent = 0,
                        photo = "default.jpg" // Fotoğraf varsayılan
                    };
                    await _customerService.CreateCustomerAsync(customer);
                }
            }

            // 5 ürün zaten veritabanında bulunmalı, ancak ürünleri kontrol et
            var existingProducts = await _productRepository.GetAllAsync();
            if (existingProducts.Count() == 0)
            {
                // Ürünler veritabanında yoksa, ekle
                var products = new List<Product>
                {
                    new Product { ProductName = "Product1", Stock = 500, Price = 100, Description = "Description of Product1", Photo = "product1.jpg", Visibility = true },
                    new Product { ProductName = "Product2", Stock = 10, Price = 200, Description = "Description of Product2", Photo = "product2.jpg", Visibility = true },
                    new Product { ProductName = "Product3", Stock = 200, Price = 75, Description = "Description of Product3", Photo = "product3.jpg", Visibility = true },
                    new Product { ProductName = "Product4", Stock = 0, Price = 50, Description = "Description of Product4", Photo = "product4.jpg", Visibility = true },
                    new Product { ProductName = "Product5", Stock = 45, Price = 45, Description = "Description of Product5", Photo = "product5.jpg", Visibility = true }
                };

                foreach (var product in products)
                {
                    await _productService.CreateProductAsync(product);
                }
            }

            return Ok("Random data successfully generated.");
        }
    }
}
