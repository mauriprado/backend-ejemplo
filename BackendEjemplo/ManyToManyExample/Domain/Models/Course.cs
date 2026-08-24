namespace BackendEjemplo.ManyToManyExample.Domain.Models
{
    public class Course
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public int Credits { get; set; }

        //lado "uno" de la relación muchos a muchos (a través de Enrollment)
        public IList<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }
}
