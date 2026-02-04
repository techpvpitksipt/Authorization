using System;
using System.Collections.Generic;

namespace MyApp;

public partial class Product
{
    public string Productarticlenumber { get; set; } = null!;

    public string Productname { get; set; } = null!;

    public string Productdescription { get; set; } = null!;

    public string Productcategory { get; set; } = null!;

    public string? Productphoto { get; set; }

    public string Productmanufacturer { get; set; } = null!;

    public decimal Productcost { get; set; }

    public short? Productdiscountamount { get; set; }

    public int Productquantityinstock { get; set; }

    public string Productstatus { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
