using System.ComponentModel.DataAnnotations;

namespace BackendEjemplo.ManyToManyExample.Resources
{
    public class SaveCourseResource
    {
        [Required, MaxLength(100)]
        public string Name { get; set; }
        [Required, MaxLength(20)]
        public string Code { get; set; }
        [Required]
        public int? Credits { get; set; }
    }
}
