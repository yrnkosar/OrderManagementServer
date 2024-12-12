using System;
using System.Collections.Generic;

namespace OrderManagement.Models;

public partial class Customer
{
    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = null!;

    public decimal Budget { get; set; }

    public string CustomerType { get; set; } = null!;

    public decimal? TotalSpent { get; set; }

    public virtual ICollection<Log> Logs { get; set; } = new List<Log>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public int WaitTimeInSeconds { get; set; }  // Bu özelliği ekleyin
}
