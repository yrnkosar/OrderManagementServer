using OrderManagement.Models;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace OrderManagement.Services
{
    public interface ICustomerService
    {
        Task<IEnumerable<Customer>> GetAllCustomersAsync();
        Task<Customer> GetCustomerByIdAsync(int id);
        Task CreateCustomerAsync(Customer customer);
        Task UpdateCustomerAsync(Customer customer);
        Task DeleteCustomerAsync(int id);
        Task<Customer> GetCurrentCustomerAsync(ClaimsPrincipal user); // Bu metodu ekliyoruz
    }
    /*
        public interface ICustomerService
        {
            Task<IEnumerable<Customer>> GetAllCustomersAsync();
            Task<Customer> GetCustomerByIdAsync(int id);
            Task CreateCustomerAsync(Customer customer);
            Task UpdateCustomerAsync(Customer customer);
            Task DeleteCustomerAsync(int id);

            Task<Customer> GetCurrentCustomerAsync(ClaimsPrincipal user);
        }*/
}
