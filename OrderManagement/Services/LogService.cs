using OrderManagement.Models;
using OrderManagement.Repositories;

namespace OrderManagement.Services
{
    public class LogService : ILogService
    {
        private readonly ILogRepository _logRepository;

        public LogService(ILogRepository logRepository)
        {
            _logRepository = logRepository;
        }

        public async Task LogOrderAsync(Log log)
        {
            await _logRepository.AddAsync(log);
        }
        public async Task LogOrderCreatedAsync(Order order)
        {
            var log = new Log
            {
                CustomerId = order.CustomerId,
                OrderId = order.OrderId,
                LogDate = DateTime.Now,
                LogType = "Bilgilendirme",
                LogDetails = $"Order {order.OrderId} oluşturuldu. Durum: {order.OrderStatus}"
            };
            await _logRepository.AddAsync(log);
        }

        public async Task<IEnumerable<Log>> GetLogsAsync()
        {
            return await _logRepository.GetAllAsync();
        }
    }
}
