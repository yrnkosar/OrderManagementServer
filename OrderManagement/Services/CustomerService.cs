using OrderManagement.Models;
using OrderManagement.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrderManagement.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;

        public CustomerService(ICustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }

        public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
        {
            var customers = await _customerRepository.GetAllAsync();
            return await _customerRepository.GetAllAsync();
        }

        public async Task<Customer> GetCustomerByIdAsync(int id)
        {
            return await _customerRepository.GetByIdAsync(id);
        }

        public async Task CreateCustomerAsync(Customer customer)
        {
            await _customerRepository.AddAsync(customer);
        }

        public async Task UpdateCustomerAsync(Customer customer)
        {
            await _customerRepository.UpdateAsync(customer);
        }

        public async Task DeleteCustomerAsync(int id)
        {
            await _customerRepository.DeleteAsync(id);
        }

        public async Task<IEnumerable<Customer>> GetPremiumCustomersAsync()
        {
            return await _customerRepository.GetPremiumCustomersAsync();
        }

        private int CalculatePriorityScore(Customer customer)
        {
            int basePriorityScore = customer.CustomerType == "Premium" ? 15 : 10;
            double waitTimeWeight = 0.5; // Bekleme süresi ağırlığı
                                         // Bekleme süresi burada varsayalım ki müşteri objesinde mevcut
            return (int)(basePriorityScore + (customer.WaitTimeInSeconds * waitTimeWeight)); // Açık dönüşüm
        }
    }
}
