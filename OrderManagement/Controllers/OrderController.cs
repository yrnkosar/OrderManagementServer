using Microsoft.AspNetCore.Mvc;
using OrderManagement.Models;
using OrderManagement.Data;
using System.Linq;
using System;

namespace OrderManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly OrderManagementContext _context;

        public OrderController(OrderManagementContext context)
        {
            _context = context;
        }

        // Place an order
        [HttpPost]
        public ActionResult<Order> PlaceOrder([FromBody] Order order)
        {
            var customer = _context.Customers.Find(order.CustomerId);
            var product = _context.Products.Find(order.ProductId);

            // Check if the customer has sufficient budget
            if (customer.Budget < order.TotalPrice)
            {
                LogTransaction("Hata", customer, order, "Müşteri bakiyesi yetersiz");
                return BadRequest("Müşteri bakiyesi yetersiz");
            }

            // Check if product stock is sufficient
            if (product.Stock < order.Quantity)
            {
                LogTransaction("Hata", customer, order, "Ürün stoğu yetersiz");
                return BadRequest("Ürün stoğu yetersiz");
            }

            // Process order
            product.Stock -= order.Quantity;
            customer.Budget -= order.TotalPrice;
            customer.TotalSpent += order.TotalPrice;

            _context.Orders.Add(order);
            _context.SaveChanges();

            LogTransaction("Bilgilendirme", customer, order, "Satın alma başarılı");

            return CreatedAtAction(nameof(GetOrder), new { id = order.OrderId }, order);
        }

        // Get order by ID
        [HttpGet("{id}")]
        public ActionResult<Order> GetOrder(int id)
        {
            var order = _context.Orders.Find(id);
            if (order == null)
                return NotFound();

            return Ok(order);
        }

        // Log transaction
        private void LogTransaction(string logType, Customer customer, Order order, string result)
        {
            var log = new Log
            {
                CustomerId = customer.CustomerId,
                OrderId = order.OrderId,
                LogDate = DateTime.Now,
                LogType = logType,
                LogDetails = result,
                CustomerType = customer.CustomerType,
                ProductName = order.Product.ProductName,
                Quantity = order.Quantity,
                Result = result
            };

            _context.Logs.Add(log);
            _context.SaveChanges();
        }
    }
}
