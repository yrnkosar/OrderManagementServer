using System.Security.Claims;
using System.Threading.Tasks;

namespace OrderManagement.Services
{
    public interface IUserService
    {
        // Giriş yapan kullanıcının ID'sini almak için metot
        Task<string> GetCurrentUserIdAsync(ClaimsPrincipal user);

        // Token'den müşteri ID'sini almak için metot
        Task<string> GetCustomerIdFromToken(string token);
    }

}
