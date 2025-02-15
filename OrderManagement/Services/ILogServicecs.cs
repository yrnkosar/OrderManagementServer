﻿using OrderManagement.Models;

namespace OrderManagement.Services
{
    public interface ILogService
    {
        Task LogOrderAsync(Log log);
        Task<IEnumerable<Log>> GetLogsByCustomerIdAsync(int customerId); 
        Task<IEnumerable<Log>> GetLogsAsync();
    }
}
