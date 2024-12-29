using Microsoft.AspNetCore.SignalR;
using OrderManagement.Hubs;
using OrderManagement.Models;
using OrderManagement.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using OrderManagement.Hubs;
using Microsoft.Data.SqlClient;

namespace OrderManagement.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IProductRepository _productRepository;
        private readonly ICustomerService _customerService;
        private readonly IUserService _userService;
        private readonly ILogService _logService; 
        private static readonly Mutex _mutex = new Mutex(); 
        private readonly IHubContext<OrderHub> _hubContext; 
        private readonly SystemStatusService _systemStatusService;

        public OrderService(IOrderRepository orderRepository, ICustomerRepository customerRepository, IProductRepository productRepository,
            ICustomerService customerService, IUserService userService, ILogService logService, IHubContext<OrderHub> hubContext, SystemStatusService systemStatusService)
        {
            _orderRepository = orderRepository;
            _customerRepository = customerRepository;
            _productRepository = productRepository;
            _customerService = customerService;
            _userService = userService;
            _logService = logService;
            _hubContext = hubContext;
            _systemStatusService = systemStatusService; 
        }

        public async Task<IEnumerable<Order>> GetAllOrdersAsync()
        {
            var allOrders = await _orderRepository.GetAllAsync();

            foreach (var order in allOrders)
            {
                if (order.OrderStatus != "Completed" && order.OrderStatus != "Cancelled")

                {
                    order.WaitingTime = (int)((DateTime.Now - order.OrderDate.GetValueOrDefault(DateTime.MinValue)).TotalSeconds); 
                    order.PriorityScore = CalculatePriorityScore(await _customerRepository.GetByIdAsync(order.CustomerId), order.OrderDate.GetValueOrDefault(DateTime.MinValue));
                    await _orderRepository.UpdateAsync(order);
                }
            }

            return allOrders;
        }
        public async Task<Order> GetOrderByIdAsync(int id)
        {
            return await _orderRepository.GetByIdAsync(id);
        }

        public async Task<Order> CreateOrderAsync(Order order, ClaimsPrincipal user)
        {
            if (await _systemStatusService.IsAdminProcessing())
            {
                return null; 
            }

            var userId = await _userService.GetCurrentUserIdAsync(user);
            if (userId == null || order.Quantity <= 0)
                return null; 

            var customerId = int.Parse(userId);

            var customer = await _customerRepository.GetByIdAsync(customerId);
            if (customer == null)
                return null; 

            var customerOrders = await _orderRepository.GetAllAsync();
            var totalProductOrders = customerOrders
                .Where(o => o.CustomerId == customerId && o.ProductId == order.ProductId && o.OrderStatus != "Cancelled")
                .Sum(o => o.Quantity);



            order.TotalPrice = order.Quantity * (await GetProductPriceAsync(order.ProductId));

            await _orderRepository.AddAsync(order);

            await _logService.LogOrderAsync(new Log
            {
                CustomerId = customer.CustomerId,
                OrderId = order.OrderId,
                LogDate = DateTime.Now,
                LogType = "Bilgilendirme",
                LogDetails = $"Müşteri {customer.CustomerId} yeni sipariş oluşturdu. Sipariş ID: {order.OrderId}, Ürün ID: {order.ProductId}, Miktar: {order.Quantity}, Fiyat: {order.TotalPrice}",
                CustomerType = customer.CustomerType,
                ProductName = (await _productRepository.GetByIdAsync(order.ProductId))?.ProductName ?? "Bilinmeyen Ürün",
                Quantity = order.Quantity,
                Result = "Yeni Sipariş"
            });

            return order;
        }


        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); 
        public async Task ApproveAllOrdersAsync()
        {
            await _semaphore.WaitAsync(); 
            try
            {
              
                var allOrders = await _orderRepository.GetAllAsync();
                var pendingOrders = allOrders.Where(order => order.OrderStatus == "Pending").ToList();

                if (!pendingOrders.Any())
                {
                    Console.WriteLine("Onaylanacak sipariş yok.");
                    return;
                }

            
                var customerTasks = pendingOrders.Select(order => _customerRepository.GetByIdAsync(order.CustomerId)).ToList();
                var customerList = await Task.WhenAll(customerTasks);

                var sortedOrders = pendingOrders
                    .Select((order, index) => new
                    {
                        Order = order,
                        PriorityScore = CalculatePriorityScore(customerList[index], order.OrderDate.GetValueOrDefault(DateTime.MinValue))
                    })
                    .OrderByDescending(order => order.PriorityScore) 
                    .Select(order => order.Order) 
                    .ToList();

                foreach (var order in sortedOrders)
                {
                    await ProcessOrderAsync(order); 
                }
            }
            finally
            {
                _semaphore.Release(); 
            }
        }
        public async Task ProcessOrderAsync(Order order)
        {
            try
            {
                var customer = await _customerRepository.GetByIdAsync(order.CustomerId);
                var product = await _productRepository.GetByIdAsync(order.ProductId);
                List<string> failureReasons = new List<string>();

                if (order.WaitingTime > 300)
                {
                    failureReasons.Add("Zaman aşımı");

                    await _logService.LogOrderAsync(new Log
                    {
                        CustomerId = order.CustomerId,
                        OrderId = order.OrderId,
                        LogDate = DateTime.Now,
                        LogType = "Hata",
                        LogDetails = $"Sipariş {order.OrderId} iptal edildi. Sebep: {string.Join(", ", failureReasons)}.",
                        CustomerType = customer.CustomerType,
                        ProductName = product.ProductName,
                        Quantity = order.Quantity,
                        Result = "Başarısız"
                    });

                    order.OrderStatus = "Cancelled";
                    await _orderRepository.UpdateAsync(order);

                    await _hubContext.Clients.Group(order.CustomerId.ToString())
                        .SendAsync("ReceiveOrderStatusUpdate", new
                        {
                            OrderId = order.OrderId,
                            Status = "Cancelled",
                            Message = "Sipariş zaman aşımı nedeniyle iptal edildi."
                        });

                    return; 
                }

                order.OrderStatus = "Processing";
                await _orderRepository.UpdateAsync(order);
                await _hubContext.Clients.Group(order.CustomerId.ToString())
                    .SendAsync("ReceiveOrderStatusUpdate", new
                    {
                        OrderId = order.OrderId,
                        Status = "Processing",
                        Message = "Sipariş işleniyor."
                    });

                await Task.Delay(5000);

                if (product == null || !product.Visibility)
                {
                    failureReasons.Add("Ürün silindi");
                }

                if (customer.Budget < order.TotalPrice)
                {
                    failureReasons.Add("Müşteri bakiyesi yetersiz");
                }

                if (product.Stock < order.Quantity)
                {
                    failureReasons.Add("Ürün stoğu yetersiz");
                }

                if (!failureReasons.Any())
                {
                    using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2)))
                    {
                        try
                        {
                            customer.Budget -= order.TotalPrice;
                            customer.TotalSpent += order.TotalPrice;
                            product.Stock -= order.Quantity;
                            order.OrderStatus = "Completed";

                            await _customerRepository.UpdateAsync(customer);
                            await _productRepository.UpdateAsync(product);
                            await _orderRepository.UpdateAsync(order);

                            if (customer.TotalSpent > 2000 && customer.CustomerType != "Premium")
                            {
                                customer.CustomerType = "Premium";
                                await _customerRepository.UpdateAsync(customer);
                            }

                            await _logService.LogOrderAsync(new Log
                            {
                                CustomerId = customer.CustomerId,
                                OrderId = order.OrderId,
                                LogDate = DateTime.Now,
                                LogType = "Bilgilendirme",
                                LogDetails = $"Sipariş {order.OrderId} başarıyla tamamlandı.",
                                CustomerType = customer.CustomerType,
                                ProductName = product.ProductName,
                                Quantity = order.Quantity,
                                Result = "Başarılı"
                            });

                            await _hubContext.Clients.Group(customer.CustomerId.ToString())
                                .SendAsync("ReceiveOrderStatusUpdate", new
                                {
                                    OrderId = order.OrderId,
                                    Status = "Completed",
                                    Message = $"Sipariş {order.OrderId} başarıyla tamamlandı."
                                });
                        }
                        catch (OperationCanceledException)
                        {
                            failureReasons.Add("Zaman aşımı");
                        }
                    }
                }

                if (failureReasons.Any())
                {
                    order.OrderStatus = "Cancelled";
                    await _orderRepository.UpdateAsync(order);

                    await _logService.LogOrderAsync(new Log
                    {
                        CustomerId = customer.CustomerId,
                        OrderId = order.OrderId,
                        LogDate = DateTime.Now,
                        LogType = "Hata",
                        LogDetails = $"Sipariş {order.OrderId} iptal edildi. Sebep: {string.Join(", ", failureReasons)}.",
                        CustomerType = customer?.CustomerType ?? "Unknown",
                        ProductName = product?.ProductName ?? "Unknown",
                        Quantity = order.Quantity,
                        Result = "Başarısız"
                    });

                    await _hubContext.Clients.Group(customer.CustomerId.ToString())
                        .SendAsync("ReceiveOrderStatusUpdate", new
                        {
                            OrderId = order.OrderId,
                            Status = "Cancelled",
                            Message = $"Sipariş {order.OrderId} iptal edildi. Sebep: {string.Join(", ", failureReasons)}"
                        });
                }
            }
            catch (SqlException)
            {
                await _logService.LogOrderAsync(new Log
                {
                    CustomerId = order.CustomerId,
                    OrderId = order.OrderId,
                    LogDate = DateTime.Now,
                    LogType = "Hata",
                    LogDetails = $"Sipariş {order.OrderId} veritabanı hatası nedeniyle başarısız oldu.",
                    CustomerType = "Unknown",
                    ProductName = "Unknown",
                    Quantity = order.Quantity,
                    Result = "Başarısız"
                });

                await _hubContext.Clients.Group(order.CustomerId.ToString())
                    .SendAsync("ReceiveOrderStatusUpdate", new
                    {
                        OrderId = order.OrderId,
                        Status = "DatabaseError",
                        Message = "Sipariş veritabanı hatası nedeniyle başarısız oldu."
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sipariş işleme hatası: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<Order>> GetPendingOrdersAsync()
        {
            var allOrders = await _orderRepository.GetAllAsync();
            var pendingOrders = allOrders.Where(order => order.OrderStatus == "Pending").ToList();

            foreach (var order in pendingOrders)
            {

                    order.WaitingTime = (int)((DateTime.Now - order.OrderDate.GetValueOrDefault(DateTime.MinValue)).TotalSeconds);
                    order.PriorityScore = CalculatePriorityScore(await _customerRepository.GetByIdAsync(order.CustomerId), order.OrderDate.GetValueOrDefault(DateTime.MinValue));
                await _orderRepository.UpdateAsync(order);

            }

            return pendingOrders;
        }

        private int CalculatePriorityScore(Customer customer, DateTime orderPlacedTime)
        {
            int basePriority = customer.CustomerType == "Premium" ? 15 : 10;
            double waitingTimeInSeconds = (DateTime.Now - orderPlacedTime).TotalSeconds;
            double waitingScore = waitingTimeInSeconds * 0.5;
            return basePriority + (int)waitingScore;
        }

        public async Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(int customerId)
        {
            var allOrders = await _orderRepository.GetAllAsync();
            return allOrders.Where(order => order.CustomerId == customerId).ToList();
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
