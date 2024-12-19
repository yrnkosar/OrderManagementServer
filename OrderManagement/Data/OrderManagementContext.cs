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
                optionsBuilder.UseSqlServer("Data Source=PC_TU�BA;Initial Catalog=OrderManagement;Integrated Security=True"); // Ba�lant� dizesini buraya ekleyin.
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Customer - Order ili�kisi: M��teri silindi�inde sipari�ler de silinir (Cascade)
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany()  // M��teriye ba�l� birden fazla sipari� olabilir.
                .HasForeignKey(o => o.CustomerId) // Foreign key'i belirtelim.
                .OnDelete(DeleteBehavior.Cascade); // M��teri silindi�inde sipari�ler de silinir.

            // Customer - Log ili�kisi: M��teri silindi�inde loglar da silinir (Cascade)
            modelBuilder.Entity<Log>()
                .HasOne(l => l.Customer)
                .WithMany()  // M��teriye ba�l� birden fazla log olabilir.
                .HasForeignKey(l => l.CustomerId) // Foreign key'i belirtelim.
                .OnDelete(DeleteBehavior.Cascade); // M��teri silindi�inde loglar da silinir.

            // Order - Product ili�kisi: �r�n silinmeye �al���ld���nda, sipari�ler k�s�tlan�r (Restrict)
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Product)
                .WithMany()  // �r�ne ba�l� birden fazla sipari� olabilir.
                .HasForeignKey(o => o.ProductId) // Foreign key'i belirtelim.
                .OnDelete(DeleteBehavior.Restrict); // �r�n silinmeden �nce sipari�ler kald�r�lmal�.

            // Log - Order ili�kisi: Sipari� silindi�inde loglar silinmez, silme k�s�tlan�r (Restrict)
            modelBuilder.Entity<Log>()
                .HasOne(l => l.Order)
                .WithMany()  // Sipari�e ba�l� birden fazla log olabilir.
                .HasForeignKey(l => l.OrderId) // Foreign key'i belirtelim.
                .OnDelete(DeleteBehavior.Restrict); // Sipari� silindi�inde loglar silinmez.

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
