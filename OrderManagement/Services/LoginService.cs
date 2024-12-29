using OrderManagement.Models;
using OrderManagement.Repositories;
using System.Linq;
using System.Threading.Tasks;

namespace OrderManagement.Services
{
    public class LoginService
    {
        private readonly ICustomerRepository _customerRepository;

        public LoginService(ICustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }

        public async Task<Customer> AuthenticateCustomerAsync(string customerName, string password)
        {
            var customer = await _customerRepository.GetAllAsync();
            var foundCustomer = customer.FirstOrDefault(c => c.CustomerName == customerName && c.CustomerPassword == password);

            if (foundCustomer != null && foundCustomer.CustomerType == "Admin")
            {
                return foundCustomer;
            }

            return foundCustomer; 
        }
    }
}
