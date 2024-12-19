using OrderManagement.Models;
using OrderManagement.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace OrderManagement.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IProductRepository _productRepository;
        private readonly ICustomerService _customerService;
        private readonly IUserService _userService;
        private readonly ILogService _logService; // Burada _logService'i ekliyoruz
        private static readonly Mutex _mutex = new Mutex(); // Mutex nesnesi

        public OrderService(IOrderRepository orderRepository, ICustomerRepository customerRepository, IProductRepository productRepository, ICustomerService customerService, IUserService userService, ILogService logService)
        {
            _orderRepository = orderRepository;
            _customerRepository = customerRepository;
            _productRepository = productRepository;
            _customerService = customerService;
            _userService = userService;
            _logService = logService; // Constructor'da _logService'i başlatıyoruz
        }

        public async Task<IEnumerable<Order>> GetAllOrdersAsync()
        {
            return await _orderRepository.GetAllAsync();
        }

        public async Task<Order> GetOrderByIdAsync(int id)
        {
            return await _orderRepository.GetByIdAsync(id);
        }

        public async Task<Order> CreateOrderAsync(Order order, ClaimsPrincipal user)
        {
            // Kullanıcı kimliğini alıyoruz
            var userId = await _userService.GetCurrentUserIdAsync(user);
            if (userId == null || order.Quantity <= 0)
                return null; // Geçersiz sipariş

            var customerId = int.Parse(userId);

            // Müşteri bilgilerini alıyoruz
            var customer = await _customerRepository.GetByIdAsync(customerId);
            if (customer == null)
                return null; // Müşteri bulunamadı

            order.TotalPrice = order.Quantity * (await GetProductPriceAsync(order.ProductId));

            await _orderRepository.AddAsync(order);  // Siparişi veri tabanına ekle
            return order;
        }

        public async Task ApproveAllOrdersAsync()
        {
            // Mutex kullanarak işlemi eş zamanlı hale getiriyoruz
            _mutex.WaitOne(); // Mutex kilidi alınır

            try
            {
                // Tüm pending (onaylanmamış) siparişleri alıyoruz
                var allOrders = await _orderRepository.GetAllAsync();
                var pendingOrders = allOrders.Where(order => order.OrderStatus == "Pending").ToList();

                // Asenkron işlemleri sırasıyla alıyoruz
                var customerTasks = pendingOrders.Select(order => _customerRepository.GetByIdAsync(order.CustomerId)).ToList();
                var customerList = await Task.WhenAll(customerTasks);

                // Siparişleri öncelik skora göre sıralıyoruz
                var sortedOrders = pendingOrders
                    .Select((order, index) => new
                    {
                        Order = order,
                        PriorityScore = CalculatePriorityScore(customerList[index], order.OrderDate.GetValueOrDefault(DateTime.MinValue))
                    })
                    .OrderBy(order => order.PriorityScore)
                    .Select(order => order.Order) // Sadece order'ları seçiyoruz
                    .ToList();

                foreach (var order in sortedOrders)
                {
                    var customer = await _customerRepository.GetByIdAsync(order.CustomerId);
                    var product = await _productRepository.GetByIdAsync(order.ProductId);
                    if (customer.Budget >= order.TotalPrice && product.Stock >= order.Quantity)
                    {
                        // Müşteri bütçesini güncelle
                        customer.Budget -= order.TotalPrice;
                        customer.TotalSpent += order.TotalPrice;
                        // Ürün stoğunu güncelle
                        product.Stock -= order.Quantity;
                        // Sipariş durumunu "Completed" olarak güncelle
                        order.OrderStatus = "Completed";

                        await _orderRepository.UpdateAsync(order);
                        await _customerRepository.UpdateAsync(customer);
                        await _productRepository.UpdateAsync(product);

                        // Log kaydını oluştur
                        await _logService.LogOrderAsync(new Log
                        {
                            CustomerId = order.CustomerId,
                            OrderId = order.OrderId,
                            LogDate = DateTime.Now,
                            LogType = "Bilgilendirme",
                            LogDetails = $"Order {order.OrderId} başarıyla onaylandı ve işleme alındı.",
                            CustomerType = customer.CustomerType,
                            ProductName = product?.ProductName ?? "Unknown",
                            Quantity = order.Quantity,
                            Result = "Success"
                        });
                    }
                    else
                    {
                        // Sipariş onaylanamadı
                        order.OrderStatus = "Cancelled";
                        await _orderRepository.UpdateAsync(order);

                        // Log kaydını oluştur
                        await _logService.LogOrderAsync(new Log
                        {
                            CustomerId = order.CustomerId,
                            OrderId = order.OrderId,
                            LogDate = DateTime.Now,
                            LogType = "Hata",
                            LogDetails = $"Order {order.OrderId} onaylanamadı. Müşteri bütçesi veya ürün stoğu yetersiz.",
                            CustomerType = customer?.CustomerType ?? "Unknown",
                            ProductName = product?.ProductName ?? "Unknown",
                            Quantity = order.Quantity,
                            Result = "Failed"
                        });
                    }
                }
            }
            finally
            {
                _mutex.ReleaseMutex(); // Mutex serbest bırakılır
            }
        }
        public async Task<IEnumerable<Order>> GetPendingOrdersAsync()
        {
            var allOrders = await _orderRepository.GetAllAsync();
            return allOrders.Where(order => order.OrderStatus == "Pending").ToList();
        }

        private int CalculatePriorityScore(Customer customer, DateTime orderPlacedTime)
        {
            int basePriority = customer.CustomerType == "Premium" ? 15 : 10;
            double waitingTimeInSeconds = (DateTime.Now - orderPlacedTime).TotalSeconds;
            double waitingScore = waitingTimeInSeconds * 0.5;
            return basePriority + (int)waitingScore;
        }

        public async Task<Customer> GetCustomerByIdAsync(int id)
        {
            return await _customerRepository.GetByIdAsync(id);
        }

        public async Task<decimal> GetProductPriceAsync(int productId)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            return product?.Price ?? 0;
        }
    }
}
