using System.Security.Claims;
using System.Threading.Tasks;

namespace OrderManagement.Services
{
    public interface IUserService
    {
       
        Task<string> GetCurrentUserIdAsync(ClaimsPrincipal user);


        Task<string> GetCustomerIdFromToken(string token);
    }

}
