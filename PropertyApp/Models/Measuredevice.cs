using System;
using System.Collections.Generic;

namespace PropertyApp.Models;

public partial class MeasureDevice
{
    public int IdMeasureDevice { get; set; }

    public int IdApartment { get; set; }

    public string DeviceType { get; set; } = null!;

    public virtual Apartment IdApartmentNavigation { get; set; } = null!;

    public virtual ICollection<Measure> Measures { get; set; } = new List<Measure>();
}
