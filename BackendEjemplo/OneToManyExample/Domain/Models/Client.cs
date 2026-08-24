namespace BackendEjemplo.OneToManyExample.Domain.Models
{
    public class Client
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime RegistrationDate { get; set; }

        //relación uno a muchos
        public IList<Order> Orders { get; set; } = new List<Order>();
    }
}
