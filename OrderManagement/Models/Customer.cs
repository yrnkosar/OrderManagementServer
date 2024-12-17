using System;
using System.Collections.Generic;

namespace OrderManagement.Models;

public partial class Customer
{
    public int CustomerId { get; set; }

    public string CustomerName { get; set; } 
    public string CustomerPassword{ get; set; } 
    public decimal Budget { get; set; }

    public string CustomerType { get; set; } 

    public decimal? TotalSpent { get; set; }


   public string photo { get; set; } = null!;
}
