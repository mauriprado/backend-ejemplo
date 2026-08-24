namespace BackendEjemplo.OneToOneExample.Domain.Models
{
    public class Employee
    {
        public long Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Position { get; set; }
        public DateOnly HireDate { get; set; }

        // Lado opcional de la relación uno a uno: un empleado puede no tener
        // todavía un perfil extendido registrado.
        public EmployeeProfile? Profile { get; set; }
    }
}
