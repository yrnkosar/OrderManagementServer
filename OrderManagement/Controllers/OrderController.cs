using Microsoft.AspNetCore.Mvc;
using OrderManagement.Models;
using OrderManagement.Data;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace OrderManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly OrderManagementContext _context;
        private static Mutex _mutex = new Mutex(); // Stok ve bütçe güncellemesi için mutex
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(3); // Maksimum eş zamanlı işlem sınırı

        public OrderController(OrderManagementContext context)
        {
            _context = context;
        }
        private int CalculatePriorityScore(Customer customer, DateTime orderPlacedTime)
        {
            int basePriority = customer.CustomerType == "Premium" ? 15 : 10;
            double waitingTimeInSeconds = (DateTime.Now - orderPlacedTime).TotalSeconds;
            double waitingScore = waitingTimeInSeconds * 0.5;
            return basePriority + (int)waitingScore;
        }

        [HttpPost("process-orders")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ProcessOrders()
        {
            var orders = _context.Orders
                .Where(o => o.OrderStatus == "Pending")
                .ToList();

            // Sıralama
            orders = orders.OrderByDescending(o =>
            {
                var customer = _context.Customers.Find(o.CustomerId);
                return CalculatePriorityScore(customer, o.OrderDate ?? DateTime.Now);
            }).ToList();

            foreach (var order in orders)
            {
                await _semaphore.WaitAsync();
                try
                {
                    ProcessOrder(order);
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            return Ok("Tüm işlemler tamamlandı.");
        }

        private void ProcessOrder(Order order)
        {
            _mutex.WaitOne(); // Kritik bölgeye erişim
            try
            {
                var product = _context.Products.Find(order.ProductId);
                if (product != null && product.Stock >= order.Quantity)
                {
                    product.Stock -= order.Quantity;
                    order.OrderStatus = "Completed";
                }
                else
                {
                    order.OrderStatus = "Cancelled";
                }
                _context.SaveChanges();
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }

        [HttpPost]
        public async Task<ActionResult<Order>> PlaceOrder([FromBody] Order order)
        {
            await _semaphore.WaitAsync();
            try
            {
                // Müşteri ve ürün kontrolü
                var customer = _context.Customers.Find(order.CustomerId);
                var product = _context.Products.Find(order.ProductId);

                if (customer == null)
                {
                    LogTransaction("Hata", null, order, "Müşteri bulunamadı");
                    return NotFound("Müşteri bulunamadı");
                }

                if (product == null)
                {
                    LogTransaction("Hata", customer, order, "Ürün bulunamadı");
                    return NotFound("Ürün bulunamadı");
                }

                // Bütçe ve stok kontrolü
                if (customer.Budget < order.TotalPrice)
                {
                    LogTransaction("Hata", customer, order, "Müşteri bakiyesi yetersiz");
                    return BadRequest("Müşteri bakiyesi yetersiz");
                }

                if (product.Stock < order.Quantity)
                {
                    LogTransaction("Hata", customer, order, "Ürün stoğu yetersiz");
                    return BadRequest("Ürün stoğu yetersiz");
                }

                // Sipariş işleme
                _mutex.WaitOne(); // Kritik bölge
                try
                {
                    product.Stock -= order.Quantity;
                    customer.Budget -= order.TotalPrice;
                    customer.TotalSpent += order.TotalPrice;

                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();

                    LogTransaction("Bilgilendirme", customer, order, "Satın alma başarılı");
                }
                finally
                {
                    _mutex.ReleaseMutex();
                }

                return CreatedAtAction(nameof(GetOrder), new { id = order.OrderId }, order);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        [HttpGet("{id}")]
        public ActionResult<Order> GetOrder(int id)
        {
            var order = _context.Orders.Find(id);
            if (order == null)
                return NotFound();

            return Ok(order);
        }

        private void LogTransaction(string logType, Customer customer, Order order, string result)
        {
            var log = new Log
            {
                OrderId = order.OrderId,
                CustomerId = customer?.CustomerId ?? 0,
                LogDate = DateTime.Now,
                LogType = logType,
                LogDetails = result,
                CustomerType = customer?.CustomerType ?? "Bilinmeyen",
                ProductName = _context.Products.Find(order.ProductId)?.ProductName ?? "Bilinmeyen Ürün",
                Quantity = order.Quantity,
                Result = result
            };

            _context.Logs.Add(log);
            _context.SaveChanges();
        }
    }
}
