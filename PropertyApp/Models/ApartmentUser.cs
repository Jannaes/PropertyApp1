using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PropertyApp.Models;

public partial class ApartmentUser
{
    public int Id { get; set; }

    public int IdApartment { get; set; }

    public int IdUser { get; set; }

    public string UserRole { get; set; } = null!;

    public DateTime FromDate { get; set; }

    public DateTime? EndDate { get; set; }

    //[ValidateNever]
    public virtual Apartment IdApartmentNavigation { get; set; } = null!;

    //[ValidateNever]
    public virtual User IdUserNavigation { get; set; } = null!;

}
