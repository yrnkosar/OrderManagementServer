using Microsoft.AspNetCore.Mvc;
using OrderManagement.Services;

namespace OrderManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly LoginService _loginService;

        public AuthController(LoginService loginService)
        {
            _loginService = loginService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            var customer = await _loginService.AuthenticateCustomerAsync(loginRequest.CustomerName, loginRequest.Password);
            if (customer == null)
                return Unauthorized("Geçersiz kullanıcı adı veya şifre.");

            return Ok(new
            {
                Message = "Başarılı giriş",
                CustomerType = customer.CustomerType
            });
        }
    }

    public class LoginRequest
    {
        public string CustomerName { get; set; }
        public string Password { get; set; }
    }
}
