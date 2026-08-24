using BackendEjemplo.OneToManyExample.Domain.Enums;
using BackendEjemplo.OneToManyExample.Domain.Models;
using BackendEjemplo.OneToManyExample.Resources;

namespace BackendEjemplo.OneToManyExample.Mapping
{
    public static class OrderMappings
    {
        public static Order ToEntity(this SaveOrderResource r) => new()
        {
            TotalAmount = r.TotalAmount!.Value,
            State = OrderState.Pending,
            ClientId = r.ClientId!.Value,
            OrderDate = DateTime.UtcNow
        };

        public static OrderResource ToResource(this Order o) => new()
        {
            Id = o.Id,
            OrderDate = o.OrderDate,
            TotalAmount = o.TotalAmount,
            State = o.State,
            Client = o.Client.ToResource()
        };
    }
}
