using System;
using System.Collections.Generic;

namespace MyApp;

public partial class Order
{
    public int Orderid { get; set; }

    public string Orderstatus { get; set; } = null!;

    public DateTime Orderdeliverydate { get; set; }

    public string Orderpickuppoint { get; set; } = null!;

    public virtual ICollection<Product> Productarticlenumbers { get; set; } = new List<Product>();
}
