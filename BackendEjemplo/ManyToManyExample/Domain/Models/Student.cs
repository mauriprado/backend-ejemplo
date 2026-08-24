namespace BackendEjemplo.ManyToManyExample.Domain.Models
{
    public class Student
    {
        public long Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }

        //lado "uno" de la relación muchos a muchos (a través de Enrollment)
        public IList<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }
}
