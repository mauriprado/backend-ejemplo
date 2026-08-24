using BackendEjemplo.ManyToManyExample.Domain.Enums;
using BackendEjemplo.ManyToManyExample.Domain.Models;
using BackendEjemplo.ManyToManyExample.Resources;

namespace BackendEjemplo.ManyToManyExample.Mapping
{
    public static class EnrollmentMappings
    {
        public static Enrollment ToEntity(this SaveEnrollmentResource r) => new()
        {
            StudentId = r.StudentId!.Value,
            CourseId = r.CourseId!.Value,
            State = EnrollmentState.Active,
            EnrollmentDate = DateTime.UtcNow
        };

        public static EnrollmentResource ToResource(this Enrollment e) => new()
        {
            Id = e.Id,
            EnrollmentDate = e.EnrollmentDate,
            State = e.State,
            Student = e.Student.ToResource(),
            Course = e.Course.ToResource()
        };
    }
}
