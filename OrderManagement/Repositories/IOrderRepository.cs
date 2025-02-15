﻿using OrderManagement.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrderManagement.Repositories
{
    public interface IOrderRepository
    {
        Task<IEnumerable<Order>> GetAllAsync();
        Task<Order> GetByIdAsync(int id);
        Task AddAsync(Order order);
        Task UpdateAsync(Order order);
        Task DeleteAsync(int id);
    }

}
