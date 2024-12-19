/*using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OrderManagement.Models;
using OrderManagement.Services;
using System.Threading.Tasks;
using OrderManagement.DTOs;
using System;
using System.Security.Claims;

namespace OrderManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]  // Kullanıcının giriş yapmış olması gerektiğini belirtir
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IUserService _userService;  // Kullanıcı servisini ekliyoruz

        public OrderController(IOrderService orderService, IUserService userService)
        {
            _orderService = orderService;
            _userService = userService;
        }

        [HttpPost("place-order")]
        public async Task<IActionResult> PlaceOrder([FromBody] OrderDTO orderDTO)
        {
            if (orderDTO == null || orderDTO.Quantity <= 0)
                return BadRequest("Geçersiz sipariş bilgisi");

            // Giriş yapan kullanıcının kimliği ile işlem yapacağız
            var user = User;  // ClaimsPrincipal'ı alıyoruz

            // Kullanıcı kimliğini doğrudan user'dan alıyoruz
            var userId = await _userService.GetCurrentUserIdAsync(user);

            if (userId == null)
                return Unauthorized("Geçersiz kullanıcı bilgisi");

            var order = new Order
            {
                ProductId = orderDTO.ProductId,
                Quantity = orderDTO.Quantity,
                OrderDate = DateTime.Now,
                OrderStatus = "Pending",
                CustomerId = int.Parse(userId)  // Kullanıcı kimliğini doğrudan kullanıyoruz
            };

            var result = await _orderService.CreateOrderAsync(order, user);  // `user` parametresini geçiriyoruz

            if (result != null)
            {
                return CreatedAtAction(nameof(GetOrder), new { id = result.OrderId }, result);
            }

            return BadRequest("Sipariş işlenirken bir hata oluştu.");
        }
        [HttpPost("{id}/approve")]
        [Authorize(Roles = "Admin")]  // Sadece admin onaylayabilir
        public async Task<IActionResult> ApproveOrder(int id)
        {
            var result = await _orderService.ApproveOrderAsync(id);

            if (result)
            {
                // Log işlemi
                var log = new Log
                {
                    CustomerId = (await _orderService.GetOrderByIdAsync(id)).CustomerId,
                    OrderId = id,
                    LogDate = DateTime.Now,
                    LogType = "Bilgilendirme",
                    LogDetails = $"Order {id} başarıyla onaylandı ve işleme alındı."
                };
                await _logService.LogOrderAsync(log);

                return Ok("Sipariş onaylandı ve müşteri bütçesi güncellendi.");
            }
            else
            {
                var log = new Log
                {
                    CustomerId = (await _orderService.GetOrderByIdAsync(id)).CustomerId,
                    OrderId = id,
                    LogDate = DateTime.Now,
                    LogType = "Hata",
                    LogDetails = $"Order {id} onaylanamadı. Müşteri bütçesi veya ürün stoğu yetersiz."
                };
                await _logService.LogOrderAsync(log);

                return BadRequest("Sipariş onaylanamadı. Bütçe veya stok yetersiz.");
            }
        }

        // Siparişi görüntüleme
        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null)
                return NotFound();

            return Ok(order);
        }

        // Kullanıcıdan müşteri kimliğini al
        private int? GetCustomerIdFromClaims()
        {
            // Claims üzerinden customer ID'yi almak
            var customerClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (customerClaim != null)
            {
                return int.Parse(customerClaim.Value); // Kullanıcı kimliği Claim'den alınır
            }

            return null; // Eğer kimlik bilgisi yoksa null döner
        }
    }
}
*/using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OrderManagement.Models;
using OrderManagement.Services;
using System.Threading.Tasks;
using OrderManagement.DTOs;
using System;
using System.Security.Claims;

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

        [HttpPost("place-order")]
        public async Task<IActionResult> PlaceOrder([FromBody] OrderDTO orderDTO)
        {
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
            catch (Exception ex)
            {
                // Hata durumunda log tutma
                await _logService.LogOrderAsync(new Log
                {
                    CustomerId = -1, // Belirli bir müşteri olmadığından -1 kullandık
                    OrderId = 0,
                    LogDate = DateTime.Now,
                    LogType = "Hata",
                    LogDetails = $"Tüm siparişlerin onaylanmasında bir hata oluştu: {ex.Message}"
                });

                return StatusCode(500, "Bir hata oluştu. Lütfen tekrar deneyin.");
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
