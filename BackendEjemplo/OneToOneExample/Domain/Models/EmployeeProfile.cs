namespace BackendEjemplo.OneToOneExample.Domain.Models
{
    // Entidad dependiente de la relación uno a uno: cada perfil pertenece
    // exactamente a un empleado (lado obligatorio de la relación).
    public class EmployeeProfile
    {
        public long Id { get; set; }
        public string Biography { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime BirthDate { get; set; }

        public Employee Employee { get; set; }
        public long EmployeeId { get; set; }
    }
}
