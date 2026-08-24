using BackendEjemplo.ManyToManyExample.Domain.Enums;

namespace BackendEjemplo.ManyToManyExample.Domain.Models
{
    // Entidad de unión explícita: representa la relación muchos a muchos entre
    // Student y Course, con datos propios (fecha de inscripción y estado).
    public class Enrollment
    {
        public long Id { get; set; }
        public DateTime EnrollmentDate { get; set; }
        public EnrollmentState State { get; set; }

        public Student Student { get; set; }
        public long StudentId { get; set; }

        public Course Course { get; set; }
        public long CourseId { get; set; }
    }
}
