
using OrderManagement.Models;
using System.Security.Claims;
using System.Threading.Tasks;

namespace OrderManagement.Services
{
    public interface IOrderService
    {
        Task<IEnumerable<Order>> GetAllOrdersAsync();
        Task<Order> GetOrderByIdAsync(int id);
        Task<Order> CreateOrderAsync(Order order, ClaimsPrincipal user);
        Task ApproveAllOrdersAsync();
        Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(int customerId);
        Task<IEnumerable<Order>> GetPendingOrdersAsync(); 
       
        Task<Customer> GetCustomerByIdAsync(int id);

        Task<decimal> GetProductPriceAsync(int productId);
    }
}
