using System;
using System.Collections.Generic;

namespace OrderManagement.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } 

    public int Stock { get; set; }

    public decimal Price { get; set; }

    public string Description { get; set; } = null!;
    public string Photo { get; set; } = null!;
 
}
