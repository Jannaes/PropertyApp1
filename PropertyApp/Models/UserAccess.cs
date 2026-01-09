using System;
using System.Collections.Generic;

namespace PropertyApp.Models;

public partial class UserAccess
{
    public int Id { get; set; }

    public int IdUser { get; set; }

    public int IdApartment { get; set; }

    public DateTime FromDate { get; set; }

    public DateTime? EndDate { get; set; }

    public virtual Apartment IdApartmentNavigation { get; set; } = null!;

    public virtual User IdUserNavigation { get; set; } = null!;
}
