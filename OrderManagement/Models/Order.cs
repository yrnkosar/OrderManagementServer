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
    public double WaitingTime { get; set; }

    public double PriorityScore { get; set; }

    public virtual Customer Customer { get; set; } = null!;



    public virtual Product Product { get; set; } = null!;
}
