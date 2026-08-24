using System.ComponentModel.DataAnnotations;

namespace BackendEjemplo.ManyToManyExample.Resources
{
    public class SaveEnrollmentResource
    {
        [Required]
        public long? StudentId { get; set; }
        [Required]
        public long? CourseId { get; set; }
    }
}
