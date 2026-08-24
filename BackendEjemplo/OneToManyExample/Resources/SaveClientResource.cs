using System.ComponentModel.DataAnnotations;

namespace BackendEjemplo.OneToManyExample.Resources
{
    public class SaveClientResource
    {
        [Required, MaxLength(100)]
        public string Name { get; set; }
        [Required, MaxLength(100)]
        public string LastName { get; set; }
        [Required, MaxLength(50)]
        public string Email { get; set; }
        [Required, MaxLength(9)]
        public string PhoneNumber { get; set; }
    }
}
