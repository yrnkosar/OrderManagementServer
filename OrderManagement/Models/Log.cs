﻿using System;
using System.Collections.Generic;

namespace OrderManagement.Models;

public partial class Log
{
    public int LogId { get; set; }

    public int? CustomerId { get; set; }

    public int? OrderId { get; set; }

    public DateTime? LogDate { get; set; }

    public string? LogType { get; set; }

    public string? LogDetails { get; set; }

    public string? CustomerType { get; set; }

    public string? ProductName { get; set; }

    public int? Quantity { get; set; }

    public string? Result { get; set; }

 
    public Order Order { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
}
