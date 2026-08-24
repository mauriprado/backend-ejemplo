using BackendEjemplo.OneToManyExample.Domain.Enums;

namespace BackendEjemplo.OneToManyExample.Resources
{
    public class OrderResource
    {
        public long Id { get; set; }
        public DateTime OrderDate { get; set; }
        public Decimal TotalAmount { get; set; }
        public OrderState State { get; set; }
        public ClientResource Client { get; set; }
    }
}
