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
        public async Task<IEnumerable<Log>> GetLogsByCustomerIdAsync(int customerId)
        {
            var logs = await _logRepository.GetAllAsync();
            return logs.Where(log => log.CustomerId == customerId);
        }
        public async Task<IEnumerable<Log>> GetLogsAsync()
        {
            return await _logRepository.GetAllAsync();
        }
    }
}
