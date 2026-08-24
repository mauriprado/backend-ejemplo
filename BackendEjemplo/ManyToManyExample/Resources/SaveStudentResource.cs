using System.ComponentModel.DataAnnotations;

namespace BackendEjemplo.ManyToManyExample.Resources
{
    public class SaveStudentResource
    {
        [Required, MaxLength(100)]
        public string FirstName { get; set; }
        [Required, MaxLength(100)]
        public string LastName { get; set; }
        [Required, MaxLength(50)]
        public string Email { get; set; }
    }
}
