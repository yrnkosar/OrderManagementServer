using Microsoft.EntityFrameworkCore;
using OrderManagement.Models;
using OrderManagement.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrderManagement.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly OrderManagementContext _context;

        public ProductRepository(OrderManagementContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _context.Products.ToListAsync();
        }

        public async Task<Product> GetByIdAsync(int id)
        {
            return await _context.Products.FindAsync(id);
        }

        public async Task AddAsync(Product product)
        {
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                throw new Exception("Ürün bulunamadı.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            { var logs = await _context.Logs.Where(l => l.OrderId != null && _context.Orders.Any(o => o.OrderId == l.OrderId && o.ProductId == id)).ToListAsync();
                if (logs.Any())
                {
                    _context.Logs.RemoveRange(logs);
                    await _context.SaveChangesAsync();
                }

                // Bağımlı siparişleri kontrol et
                var orders = await _context.Orders.Where(o => o.ProductId == id).ToListAsync();
                if (orders.Any())
                {
                    // Bağımlı siparişleri sil
                    _context.Orders.RemoveRange(orders);
                    await _context.SaveChangesAsync();
                }

                // Ürünle ilişkili logları sil
               
                // Ürünü sil
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                // İşlem başarılı ise commit et
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                // Hata durumunda rollback yap
                await transaction.RollbackAsync();
                throw new Exception("Ürün silinirken hata oluştu.", ex);
            }
        }

    }
}

