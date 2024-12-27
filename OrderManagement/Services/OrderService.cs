﻿using Microsoft.AspNetCore.SignalR;
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
        private readonly ILogService _logService; // Burada _logService'i ekliyoruz
        private static readonly Mutex _mutex = new Mutex(); // Mutex nesnesi
        private readonly IHubContext<OrderHub> _hubContext; // SignalR hub context
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
            _systemStatusService = systemStatusService; // SystemStatusService'i başlatıyoruz
        }

        public async Task<IEnumerable<Order>> GetAllOrdersAsync()
        {
            var allOrders = await _orderRepository.GetAllAsync();

            foreach (var order in allOrders)
            {
                if (order.OrderStatus != "Completed" && order.OrderStatus != "Cancelled")

                {
                    order.WaitingTime = (int)((DateTime.Now - order.OrderDate.GetValueOrDefault(DateTime.MinValue)).TotalSeconds); // Bekleme süresi (saniye cinsinden)
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
                return null; // Sipariş oluşturulamayacak durumda
            }


            // Kullanıcı kimliğini alıyoruz
            var userId = await _userService.GetCurrentUserIdAsync(user);
            if (userId == null || order.Quantity <= 0)
                return null; // Geçersiz sipariş

            var customerId = int.Parse(userId);

            // Müşteri bilgilerini alıyoruz
            var customer = await _customerRepository.GetByIdAsync(customerId);
            if (customer == null)
                return null; // Müşteri bulunamadı

            // Müşterinin aynı üründen toplam sipariş miktarını kontrol ediyoruz
            var customerOrders = await _orderRepository.GetAllAsync();
            var totalProductOrders = customerOrders
                .Where(o => o.CustomerId == customerId && o.ProductId == order.ProductId && o.OrderStatus != "Cancelled")
                .Sum(o => o.Quantity);

            //if (totalProductOrders + order.Quantity > 5)
            //return null; // Aynı üründen toplamda 5'i aşan siparişe izin verme

            order.TotalPrice = order.Quantity * (await GetProductPriceAsync(order.ProductId));

            // Siparişi veri tabanına ekliyoruz
            await _orderRepository.AddAsync(order);

            // Sipariş başarıyla eklenmişse log kaydını tutuyoruz
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


        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // Aynı anda bir işlem için izin verir.
        public async Task ApproveAllOrdersAsync()
        {
            await _semaphore.WaitAsync(); // Kilidi al
            try
            {
                // Tüm "Pending" (onaylanmamış) siparişleri alıyoruz
                var allOrders = await _orderRepository.GetAllAsync();
                var pendingOrders = allOrders.Where(order => order.OrderStatus == "Pending").ToList();

                if (!pendingOrders.Any())
                {
                    Console.WriteLine("Onaylanacak sipariş yok.");
                    return;
                }

                // Asenkron işlemleri sırasıyla ele alıyoruz
                var customerTasks = pendingOrders.Select(order => _customerRepository.GetByIdAsync(order.CustomerId)).ToList();
                var customerList = await Task.WhenAll(customerTasks);

                // Siparişleri öncelik skora göre sıralıyoruz
                var sortedOrders = pendingOrders
                    .Select((order, index) => new
                    {
                        Order = order,
                        PriorityScore = CalculatePriorityScore(customerList[index], order.OrderDate.GetValueOrDefault(DateTime.MinValue))
                    })
                    .OrderByDescending(order => order.PriorityScore) // Önceliğe göre azalan sıralama (en yüksek öncelik ilk sırada)
                    .Select(order => order.Order) // Sadece order'ları seçiyoruz
                    .ToList();

                foreach (var order in sortedOrders)
                {
                    await ProcessOrderAsync(order); // Her bir siparişi sırasıyla işliyoruz
                   
                }
            }
            finally
            {
                _semaphore.Release(); // Kilidi serbest bırak
            }
        }
        public async Task ProcessOrderAsync(Order order)
        {
            try
            {
                var customer = await _customerRepository.GetByIdAsync(order.CustomerId);
                var product = await _productRepository.GetByIdAsync(order.ProductId);
                // Siparişin bekleme süresi 2 dakikayı geçmiş mi kontrol edin
                List<string> failureReasons = new List<string>();

                if (order.WaitingTime > 120)
                {
                    failureReasons.Add("Zaman aşımı");

                    // Zaman aşımı logu oluştur
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

                    // Siparişi "Cancelled" olarak işaretleyin
                    order.OrderStatus = "Cancelled";
                    await _orderRepository.UpdateAsync(order);

                    // Müşteriye ve admin'e SignalR ile zaman aşımı mesajı gönderin
                    await _hubContext.Clients.Group(order.CustomerId.ToString())
                        .SendAsync("ReceiveOrderStatusUpdate", new
                        {
                            OrderId = order.OrderId,
                            Status = "Cancelled",
                            Message = "Sipariş zaman aşımı nedeniyle iptal edildi."
                        });

                    return; // İşleme devam etmiyoruz
                }

                // Siparişi "Processing" durumuna geçirin ve SignalR ile müşteriye bildirin
                order.OrderStatus = "Processing";
                await _orderRepository.UpdateAsync(order);
                await _hubContext.Clients.Group(order.CustomerId.ToString())
                    .SendAsync("ReceiveOrderStatusUpdate", new
                    {
                        OrderId = order.OrderId,
                        Status = "Processing",
                        Message = "Sipariş işleniyor."
                    });

                await Task.Delay(5000); // Simülasyon için bekleme


                // Ürün görünürlüğünü kontrol edin
                if (product == null || !product.Visibility)
                {
                    failureReasons.Add("Ürün silindi");
                }

                // İşlem kontrolleri
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

            // Her pending sipariş için priorityScore ve waitingTime'ı güncelliyoruz
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
        // OrderService'e gerekli metot eklendi
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
