using BackendEjemplo.ManyToManyExample.Domain.Enums;

namespace BackendEjemplo.ManyToManyExample.Resources
{
    public class EnrollmentResource
    {
        public long Id { get; set; }
        public DateTime EnrollmentDate { get; set; }
        public EnrollmentState State { get; set; }
        public StudentResource Student { get; set; }
        public CourseResource Course { get; set; }
    }
}
