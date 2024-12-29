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

    public async Task<string> GetCurrentUserIdAsync(ClaimsPrincipal user)
    {
    
        var customerIdClaim = user?.FindFirst("CustomerId")?.Value;

        if (string.IsNullOrEmpty(customerIdClaim))
        {
            customerIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        return customerIdClaim;
    }

    public async Task<string> GetCustomerIdFromToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return null;

        var jwtHandler = new JwtSecurityTokenHandler();
        var jwtToken = jwtHandler.ReadJwtToken(token);
        var customerId = jwtToken?.Claims.FirstOrDefault(c => c.Type == "customerId")?.Value;

        return customerId;
    }
}
