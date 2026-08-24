using BackendEjemplo.Shared.Domain.Services.Communication;

namespace BackendEjemplo.OneToOneExample.Domain.Services.Communication
{
    public class EmployeePageRequest: BasePageRequest
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Position { get; set; }
        public DateOnly? StartHireDate { get; set; }
        public DateOnly? EndHireDate { get; set; }
    }
}
