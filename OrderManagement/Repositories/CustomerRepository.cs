using OrderManagement.Models;
using OrderManagement.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using OrderManagement.Data;

namespace OrderManagement.Repositories
{
    public class CustomerRepository : Repository<Customer>, ICustomerRepository
    {
        public CustomerRepository(OrderManagementContext context) : base(context)
        {
        }

        // Ekstra işlemler burada yapılabilir (örneğin Premium müşteriler için özel sorgular).
        public async Task<IEnumerable<Customer>> GetPremiumCustomersAsync()
        {
            return await _context.Customers.Where(c => c.CustomerType == "Premium").ToListAsync();
        }
    }
}
