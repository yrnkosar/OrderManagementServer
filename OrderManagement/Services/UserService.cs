using OrderManagement.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

public class UserService : IUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // Giriş yapan kullanıcının ID'sini almak için metod
    public async Task<string> GetCurrentUserIdAsync(ClaimsPrincipal user)
    {
        // "customerId" claim'ini buluyoruz
        var customerIdClaim = user?.FindFirst("CustomerId")?.Value;

        if (string.IsNullOrEmpty(customerIdClaim))
        {
            // Eğer "customerId" claim'i yoksa, alternatif olarak "sub" veya "NameIdentifier" da kullanılabilir
            customerIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        return customerIdClaim;
    }


    // Token'den müşteri ID'sini almak için metod
    public async Task<string> GetCustomerIdFromToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return null;

        // Token'ı çözümlemek için JwtSecurityToken kullanıyoruz
        var jwtHandler = new JwtSecurityTokenHandler();
        var jwtToken = jwtHandler.ReadJwtToken(token);

        // Token'den "customerId" claim'ini alıyoruz
        var customerId = jwtToken?.Claims.FirstOrDefault(c => c.Type == "customerId")?.Value;

        return customerId;
    }
}
