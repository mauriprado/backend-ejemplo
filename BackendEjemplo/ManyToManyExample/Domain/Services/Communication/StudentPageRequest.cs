using BackendEjemplo.Shared.Domain.Services.Communication;

namespace BackendEjemplo.ManyToManyExample.Domain.Services.Communication
{
    public class StudentPageRequest: BasePageRequest
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
    }
}
