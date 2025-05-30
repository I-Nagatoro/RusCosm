using System;

namespace Lopushok.Models;

public class ProductSalesDAO
{
    public int id { get; set; }
    public int product_id { get; set; }
    public DateOnly sale_date { get; set; }
}