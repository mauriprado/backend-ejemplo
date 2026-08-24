using BackendEjemplo.OneToManyExample.Domain.Enums;

namespace BackendEjemplo.OneToManyExample.Domain.Models
{
    public class Order
    {
        public long Id { get; set; }
        public DateTime OrderDate { get; set; }
        public Decimal TotalAmount { get; set; }
        public OrderState State { get; set; }

        //relacion uno a muchos
        public Client Client { get; set; }
        public long ClientId { get; set; }
    }
}
