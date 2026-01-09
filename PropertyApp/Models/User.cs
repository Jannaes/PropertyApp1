using System;
using System.Collections.Generic;

namespace PropertyApp.Models;

public partial class User
{
    public int IdUser { get; set; }

    public string Firstname { get; set; } = null!;

    public string Lastname { get; set; } = null!;

    public string? Streetname { get; set; }

    public string? Streetnumber { get; set; }

    public string? Apartment { get; set; }

    public string? Postcode { get; set; }

    public string? PostOfficeName { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }

    public virtual ICollection<ApartmentUser> ApartmentUsers { get; set; } = new List<ApartmentUser>();

    public virtual ICollection<Measure> Measures { get; set; } = new List<Measure>();

    public virtual ICollection<Property> Properties { get; set; } = new List<Property>();

    public virtual ICollection<UserAccess> UserAccesses { get; set; } = new List<UserAccess>();
}
