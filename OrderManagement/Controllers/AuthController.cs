using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Claims;
using OrderManagement.Models;
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

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, customer.CustomerName),
        new Claim(ClaimTypes.Role, customer.CustomerType)
    };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your-secret-key-should-be-at-least-16-characters-long"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "MyApp",
                audience: "MyAPI",
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds
            );

            return Ok(new
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token)
            });
        }


        public class LoginRequest
        {
            public string CustomerName { get; set; }
            public string Password { get; set; }
        }
    }
}
