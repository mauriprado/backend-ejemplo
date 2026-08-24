using BackendEjemplo.ManyToManyExample.Domain.Enums;
using BackendEjemplo.Shared.Domain.Services.Communication;

namespace BackendEjemplo.ManyToManyExample.Domain.Services.Communication
{
    public class EnrollmentPageRequest: BasePageRequest
    {
        public long? StudentId { get; set; }
        public long? CourseId { get; set; }
        public EnrollmentState? State { get; set; }
        public DateOnly? StartEnrollmentDate { get; set; }
        public DateOnly? EndEnrollmentDate { get; set; }
    }
}
