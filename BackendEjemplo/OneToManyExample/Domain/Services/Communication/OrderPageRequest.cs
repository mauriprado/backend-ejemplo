using BackendEjemplo.OneToManyExample.Domain.Enums;
using BackendEjemplo.Shared.Domain.Services.Communication;

namespace BackendEjemplo.OneToManyExample.Domain.Services.Communication
{
    public class OrderPageRequest: BasePageRequest
    {
        public DateOnly? StartOrderDate { get; set; }
        public DateOnly? EndOrderDate { get; set; }
        public OrderState? OrderState { get; set; }
        public string? ClientFullName { get; set; }
    }
}
