using Microsoft.EntityFrameworkCore;
using OrderManagement.Data;
using OrderManagement.Models;

namespace OrderManagement.Repositories
{
    public class LogRepository : ILogRepository
    {
        private readonly OrderManagementContext _context;

        public LogRepository(OrderManagementContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Log log)
        {
            await _context.Logs.AddAsync(log);
            await _context.SaveChangesAsync();
        }
        public async Task<IEnumerable<Log>> GetAllAsync()
        {
            return await _context.Logs.ToListAsync();  
        }
    }
}
