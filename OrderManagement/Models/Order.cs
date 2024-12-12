using System;
using System.Collections.Generic;

namespace OrderManagement.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int CustomerId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal TotalPrice { get; set; }

    public DateTime? OrderDate { get; set; }

    public string? OrderStatus { get; set; }

    public virtual Customer Customer { get; set; } = null!;

   // public virtual ICollection<Log> Logs { get; set; } = new List<Log>();

    public virtual Product Product { get; set; } = null!;
}
