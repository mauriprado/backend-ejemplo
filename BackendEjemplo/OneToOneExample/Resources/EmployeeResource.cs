namespace BackendEjemplo.OneToOneExample.Resources
{
    public class EmployeeResource
    {
        public long Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Position { get; set; }
        public DateOnly HireDate { get; set; }
    }
}
