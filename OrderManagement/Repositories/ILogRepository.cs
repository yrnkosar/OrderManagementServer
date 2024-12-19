using OrderManagement.Models;

namespace OrderManagement.Repositories
{
    public interface ILogRepository
    {
        Task AddAsync(Log log);
        Task<IEnumerable<Log>> GetAllAsync();
    }
}
