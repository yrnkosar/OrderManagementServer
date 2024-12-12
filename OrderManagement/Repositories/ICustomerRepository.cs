using Microsoft.EntityFrameworkCore;
using OrderManagement.Data;
using OrderManagement.Models;

namespace OrderManagement.Repositories
{
    public interface ICustomerRepository : IRepository<Customer>
    {
        Task<IEnumerable<Customer>> GetPremiumCustomersAsync();
    }

    public class CustomerRepository : Repository<Customer>, ICustomerRepository
    {
        public CustomerRepository(OrderManagementContext context) : base(context) { }

        public async Task<IEnumerable<Customer>> GetPremiumCustomersAsync()
        {
            return await _context.Customers.Where(c => c.CustomerType == "Premium").ToListAsync();
        }
    }
}