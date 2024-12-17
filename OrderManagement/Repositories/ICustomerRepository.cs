using OrderManagement.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrderManagement.Repositories
{
    public interface ICustomerRepository : IRepository<Customer>
    {
        Task<IEnumerable<Customer>> GetPremiumCustomersAsync();
    }
}
