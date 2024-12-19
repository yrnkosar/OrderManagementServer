using OrderManagement.Models;

namespace OrderManagement.Services
{
    public interface ILogService
    {
        Task LogOrderAsync(Log log);
        Task<IEnumerable<Log>> GetLogsAsync();
    }
}
