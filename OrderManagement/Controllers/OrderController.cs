using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OrderManagement.Models;
using OrderManagement.Services;
using System.Threading.Tasks;
using OrderManagement.DTOs;
using System;
using System.Security.Claims;
using Microsoft.Data.SqlClient;

namespace OrderManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IUserService _userService;
        private readonly ILogService _logService; // Log servisi eklendi

        public OrderController(IOrderService orderService, IUserService userService, ILogService logService)
        {
            _orderService = orderService;
            _userService = userService;
            _logService = logService; // Log servisi bağlandı
        }
        [HttpGet("all-orders")]
        [Authorize(Roles = "Admin")]  // Sadece adminler bu endpoint'e erişebilir
        public async Task<ActionResult<IEnumerable<Order>>> GetAllOrders()
        {
            var allOrders = await _orderService.GetAllOrdersAsync();

            if (allOrders == null || !allOrders.Any())
                return NotFound("Hiç sipariş bulunamadı.");

            return Ok(allOrders);
        }

        [HttpPost("place-order")]
        public async Task<IActionResult> PlaceOrder([FromBody] OrderDTO orderDTO)
        {
            Console.WriteLine("OrderDTO içeriği:");
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(orderDTO));

            if (orderDTO == null || orderDTO.Quantity <= 0)
                return BadRequest("Geçersiz sipariş bilgisi");

            var user = User;
            var userId = await _userService.GetCurrentUserIdAsync(user);

            if (userId == null)
                return Unauthorized("Geçersiz kullanıcı bilgisi");

            var order = new Order
            {
                ProductId = orderDTO.ProductId,
                Quantity = orderDTO.Quantity,
                OrderDate = DateTime.Now,
                OrderStatus = "Pending",
                CustomerId = int.Parse(userId)
            };

            var result = await _orderService.CreateOrderAsync(order, user);

            if (result != null)
            {
                return CreatedAtAction(nameof(GetOrder), new { id = result.OrderId }, result);
            }

            return BadRequest("Sipariş işlenirken bir hata oluştu.");
        }
            // Siparişleri onaylama işlemi
        [HttpPost("approve-all-orders")]
        [Authorize(Roles = "Admin")]  // Sadece adminlerin onaylama yetkisi olsun
        public async Task<IActionResult> ApproveAllOrders()
        { 
            try
            {
                // Tüm siparişleri onaylama işlemi
                await _orderService.ApproveAllOrdersAsync();

                return Ok("Tüm siparişler başarıyla onaylandı.");
            }
            catch (SqlException sqlEx)
            {
                Console.WriteLine($"SQL Hatası: {sqlEx.Message}");
                return StatusCode(500, $"SQL Hatası: {sqlEx.Message}");
            }
      
        }

        [HttpGet("pending-orders")]
        [Authorize(Roles = "Admin")]  // Sadece adminler bu endpoint'e erişebilir
        public async Task<ActionResult<IEnumerable<Order>>> GetPendingOrders()
        {
            var pendingOrders = await _orderService.GetPendingOrdersAsync();

            if (pendingOrders == null || !pendingOrders.Any())
                return NotFound("Pending sipariş bulunamadı.");

            return Ok(pendingOrders);
        }
        [HttpGet("my-orders")]
        public async Task<ActionResult<IEnumerable<Order>>> GetMyOrders()
        {
            // Oturum açmış kullanıcının kimliğini al
            var user = User;
            var userId = await _userService.GetCurrentUserIdAsync(user);

            if (userId == null)
                return Unauthorized("Geçersiz kullanıcı bilgisi");

            // Kullanıcının siparişlerini al
            var myOrders = await _orderService.GetOrdersByCustomerIdAsync(int.Parse(userId));

            if (myOrders == null || !myOrders.Any())
                return NotFound("Hiç sipariş bulunamadı.");

            return Ok(myOrders);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null)
                return NotFound();

            return Ok(order);
        }
    }
}
