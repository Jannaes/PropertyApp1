using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyApp.Models;

public partial class Measure
{
    public long MeasureId { get; set; }

    public int IdMeasureDevice { get; set; }

    public int IdUser { get; set; }

    public decimal Amount { get; set; }

    [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}")]
    public DateTime Date { get; set; }


    //public virtual MeasureDevice IdMeasureDeviceNavigation { get; set; } = null!;  // nämä poistettu, koska tallennus ei onnistunut: The IdUserNavigation field is required ja The IdMeasureDeviceNavigation field is required.

    //public virtual User IdUserNavigation { get; set; } = null!;

    public virtual MeasureDevice? IdMeasureDeviceNavigation { get; set; }
    public virtual User? IdUserNavigation { get; set; }

    [NotMapped] // EF ei tallenna tätä tietokantaan
    public decimal? Change { get; set; }



}
