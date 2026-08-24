using System.ComponentModel.DataAnnotations;

namespace BackendEjemplo.OneToOneExample.Resources
{
    public class SaveEmployeeProfileResource
    {
        [Required, MaxLength(500)]
        public string Biography { get; set; }
        [Required, MaxLength(200)]
        public string Address { get; set; }
        [Required, MaxLength(9)]
        public string PhoneNumber { get; set; }
        [Required]
        public DateTime? BirthDate { get; set; }
        [Required]
        public long? EmployeeId { get; set; }
    }
}
