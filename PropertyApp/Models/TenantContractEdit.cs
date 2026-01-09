using System;
using System.ComponentModel.DataAnnotations;

namespace PropertyApp.Models
{
    public class TenantContractEdit
    {
        public int Id { get; set; }

        [Required]
        public int IdApartment { get; set; }

        [Required]
        public int IdUser { get; set; }

        [Required]
        [StringLength(50)]
        public string UserRole { get; set; } = "tenant";

        [Required(ErrorMessage = "From date is required")]
        [DataType(DataType.Date)]
        public DateTime FromDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }
    }
}