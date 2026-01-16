using System;
using System.Collections.Generic;

namespace PropertyApp.Models;

public partial class Apartment
{
    public int IdApartment { get; set; }

    public int IdProperty { get; set; }

    public string? StaircaseDoor { get; set; }

    public virtual ICollection<ApartmentUser> ApartmentUsers { get; set; } = new List<ApartmentUser>();

    public virtual Property IdPropertyNavigation { get; set; } = null!;

    public virtual ICollection<MeasureDevice> Measuredevices { get; set; } = new List<MeasureDevice>();

    public virtual ICollection<UserAccess> UserAccesses { get; set; } = new List<UserAccess>();

 
}
