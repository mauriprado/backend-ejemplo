using BackendEjemplo.Shared.Domain.Services.Communication;

namespace BackendEjemplo.ManyToManyExample.Domain.Services.Communication
{
    public class CoursePageRequest: BasePageRequest
    {
        public string? Name { get; set; }
        public string? Code { get; set; }
    }
}
