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

     
        public async Task<IEnumerable<Customer>> GetPremiumCustomersAsync()
        {
            return await _context.Customers.Where(c => c.CustomerType == "Premium").ToListAsync();
        }
        public async Task AddAsync(Customer customer)
        {
            await _context.Customers.AddAsync(customer);  
            await _context.SaveChangesAsync();            
        }
    }
}
