using Microsoft.EntityFrameworkCore;
using OrderManagement.Models;

namespace OrderManagement.Data
{
    public partial class OrderManagementContext : DbContext
    {
        public OrderManagementContext()
        {
        }

        public OrderManagementContext(DbContextOptions<OrderManagementContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Customer> Customers { get; set; } = null!;
        public virtual DbSet<Product> Products { get; set; } = null!;
        public virtual DbSet<Order> Orders { get; set; } = null!;
        public virtual DbSet<Log> Logs { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Data Source=PC_TUÐBA;Initial Catalog=OrderManagement;Integrated Security=True"); // Baðlantý dizesini buraya ekleyin.
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
          
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany()  
                .HasForeignKey(o => o.CustomerId) 
                .OnDelete(DeleteBehavior.Cascade); 

           
            modelBuilder.Entity<Log>()
                .HasOne(l => l.Customer)
                .WithMany()  
                .HasForeignKey(l => l.CustomerId) 
                .OnDelete(DeleteBehavior.Cascade);

           
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Product)
                .WithMany() 
                .HasForeignKey(o => o.ProductId) 
                .OnDelete(DeleteBehavior.Restrict); 

            
            modelBuilder.Entity<Log>()
                .HasOne(l => l.Order)
                .WithMany()  
                .HasForeignKey(l => l.OrderId) 
                .OnDelete(DeleteBehavior.Restrict); 

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
