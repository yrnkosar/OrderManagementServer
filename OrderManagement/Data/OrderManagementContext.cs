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
            // Customer - Order iliþkisi: Müþteri silindiðinde sipariþler de silinir (Cascade)
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany()  // Müþteriye baðlý birden fazla sipariþ olabilir.
                .HasForeignKey(o => o.CustomerId) // Foreign key'i belirtelim.
                .OnDelete(DeleteBehavior.Cascade); // Müþteri silindiðinde sipariþler de silinir.

            // Customer - Log iliþkisi: Müþteri silindiðinde loglar da silinir (Cascade)
            modelBuilder.Entity<Log>()
                .HasOne(l => l.Customer)
                .WithMany()  // Müþteriye baðlý birden fazla log olabilir.
                .HasForeignKey(l => l.CustomerId) // Foreign key'i belirtelim.
                .OnDelete(DeleteBehavior.Cascade); // Müþteri silindiðinde loglar da silinir.

            // Order - Product iliþkisi: Ürün silinmeye çalýþýldýðýnda, sipariþler kýsýtlanýr (Restrict)
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Product)
                .WithMany()  // Ürüne baðlý birden fazla sipariþ olabilir.
                .HasForeignKey(o => o.ProductId) // Foreign key'i belirtelim.
                .OnDelete(DeleteBehavior.Restrict); // Ürün silinmeden önce sipariþler kaldýrýlmalý.

            // Log - Order iliþkisi: Sipariþ silindiðinde loglar silinmez, silme kýsýtlanýr (Restrict)
            modelBuilder.Entity<Log>()
                .HasOne(l => l.Order)
                .WithMany()  // Sipariþe baðlý birden fazla log olabilir.
                .HasForeignKey(l => l.OrderId) // Foreign key'i belirtelim.
                .OnDelete(DeleteBehavior.Restrict); // Sipariþ silindiðinde loglar silinmez.

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
