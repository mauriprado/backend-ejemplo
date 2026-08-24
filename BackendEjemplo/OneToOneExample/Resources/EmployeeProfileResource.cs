namespace BackendEjemplo.OneToOneExample.Resources
{
    public class EmployeeProfileResource
    {
        public long Id { get; set; }
        public string Biography { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime BirthDate { get; set; }
        public EmployeeResource Employee { get; set; }
    }
}
