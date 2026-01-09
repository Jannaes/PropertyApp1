using System;
using System.Collections.Generic;

namespace PropertyApp.Models;

public partial class Property
{
    public int IdProperty { get; set; }

    public int IdUser { get; set; }

    public string? Name { get; set; }

    public string? Streetname { get; set; }

    public string? Streetnumber { get; set; }

    public string? Postcode { get; set; }

    public string? PostOfficeName { get; set; }

    public virtual ICollection<Apartment> Apartments { get; set; } = new List<Apartment>();

    public virtual User IdUserNavigation { get; set; } = null!;
}
