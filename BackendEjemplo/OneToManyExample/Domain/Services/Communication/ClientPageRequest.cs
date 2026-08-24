using BackendEjemplo.Shared.Domain.Services.Communication;

namespace BackendEjemplo.OneToManyExample.Domain.Services.Communication
{
    public class ClientPageRequest: BasePageRequest
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public DateOnly? StartRegistrationDate { get; set; }
        public DateOnly? EndRegistrationDate { get; set; }
    }
}
