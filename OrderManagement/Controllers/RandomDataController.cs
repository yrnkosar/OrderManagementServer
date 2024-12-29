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
        private readonly Random _rand;

        public RandomDataController(ICustomerService customerService, IProductService productService,
                                     ICustomerRepository customerRepository, IProductRepository productRepository)
        {
            _customerService = customerService;
            _productService = productService;
            _customerRepository = customerRepository;
            _productRepository = productRepository;
            _rand = new Random(); 
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateRandomData()
        {
            try
            {
                
                var existingCustomers = await _customerRepository.GetAllAsync();
                int currentCustomerCount = existingCustomers.Count();

                int customerCount = _rand.Next(5, 11);
                if (currentCustomerCount < customerCount)
                {
                  
                    for (int i = currentCustomerCount; i < customerCount; i++)
                    {
                        var customer = new Customer
                        {
                            CustomerName = "Customer" + (i + 1),
                            Budget = _rand.Next(500, 3001), 
                            CustomerType = (i < 2) ? "Premium" : "Standard", 
                            TotalSpent = 0,
                            photo = "default.jpg" 
                        };

                        try
                        {
                            
                            await _customerService.CreateCustomerAsync(customer);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error while adding customer: {ex.Message}");
                            return StatusCode(500, $"Error while adding customer: {ex.Message}");
                        }
                    }
                }

           
                var existingProducts = await _productRepository.GetAllAsync();
                if (existingProducts.Count() == 0)
                {
                   
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
                        try
                        {
                            
                            await _productService.CreateProductAsync(product);
                        }
                        catch (Exception ex)
                        {
                          
                            Console.WriteLine($"Error while adding product: {ex.Message}");
                            return StatusCode(500, $"Error while adding product: {ex.Message}");
                        }
                    }
                }

                return Ok("Random data successfully generated.");
            }
            catch (Exception ex)
            {
              
                Console.WriteLine($"Error in GenerateRandomData: {ex.Message}");
                return StatusCode(500, $"Error in GenerateRandomData: {ex.Message}");
            }
        }
    }
}
