using OrderManagement.Models;
using OrderManagement.Repositories;
using System.Security.Claims;
using System.Threading.Tasks;

namespace OrderManagement.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IUserService _userService;

        public CustomerService(ICustomerRepository customerRepository, IUserService userService)
        {
            _customerRepository = customerRepository;
            _userService = userService;
        }

        public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
        {
            return await _customerRepository.GetAllAsync();
        }

        public async Task<Customer> GetCustomerByIdAsync(int id)
        {
            return await _customerRepository.GetByIdAsync(id);
        }

        public async Task CreateCustomerAsync(Customer customer)
        {
            customer.TotalSpent = 0; // TotalSpent başlatılıyor.
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

        public async Task<Customer> GetCurrentCustomerAsync(ClaimsPrincipal user)
        {
            // Kullanıcı kimliğini alıyoruz
            var userId = await _userService.GetCurrentUserIdAsync(user);
            if (userId == null)
                return null; // Geçersiz kullanıcı kimliği

            // Kullanıcı kimliği ile müşteri bilgilerini alıyoruz
            return await _customerRepository.GetByIdAsync(int.Parse(userId));
        }


    }
}
