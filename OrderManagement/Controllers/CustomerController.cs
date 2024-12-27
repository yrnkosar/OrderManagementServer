using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Models;
using OrderManagement.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace OrderManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly IUserService _userService;
        public CustomerController(ICustomerService customerService, IUserService userService)
        {
            _customerService = customerService;
            _userService = userService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllCustomers()
        {
            var customers = await _customerService.GetAllCustomersAsync();
            return Ok(customers);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCustomerById(int id)
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null) return NotFound();
            return Ok(customer);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomer(Customer customer)
        {
            await _customerService.CreateCustomerAsync(customer);
            return CreatedAtAction(nameof(GetCustomerById), new { id = customer.CustomerId }, customer);
        }


        [HttpPut("update-profile")]
        public async Task<IActionResult> UpdateCustomerProfile([FromBody] Customer updatedCustomer)
        {
            // Oturum açmış kullanıcının kimliğini al
            var user = User;
            var userIdClaim = await _userService.GetCurrentUserIdAsync(user);

            // Geçerli bir kullanıcı kimliği alındı mı?
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Geçersiz kullanıcı bilgisi");

            // userId'yi int'e dönüştür
            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized("Geçersiz kullanıcı kimliği.");
            }

            // Güncellenmek istenen müşteri kimliği ile giriş yapan kullanıcı kimliği eşleşiyor mu?
            if (userId != updatedCustomer.CustomerId)
            {
                return Forbid("Sadece kendi bilgilerinizi güncelleyebilirsiniz.");
            }

            // Kullanıcıyı veritabanından al
            var existingCustomer = await _customerService.GetCustomerByIdAsync(userId);
            if (existingCustomer == null)
            {
                return NotFound("Müşteri bulunamadı.");
            }

            // Güncelleme işlemi
            existingCustomer.CustomerName = updatedCustomer.CustomerName;
            existingCustomer.CustomerPassword = updatedCustomer.CustomerPassword;
            existingCustomer.Budget = updatedCustomer.Budget;
            existingCustomer.photo = updatedCustomer.photo;

            // Veritabanını güncelle
            await _customerService.UpdateCustomerAsync(existingCustomer);

            return Ok("Profil güncellendi.");
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            await _customerService.DeleteCustomerAsync(id);
            return NoContent();
        }
    }
}
