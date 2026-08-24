using BackendEjemplo.Shared.Domain.Services.Communication;

namespace BackendEjemplo.OneToOneExample.Domain.Services.Communication
{
    public class EmployeeProfilePageRequest: BasePageRequest
    {
        public long? EmployeeId { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
