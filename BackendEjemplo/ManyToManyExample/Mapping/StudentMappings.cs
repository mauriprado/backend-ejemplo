using BackendEjemplo.ManyToManyExample.Domain.Models;
using BackendEjemplo.ManyToManyExample.Resources;

namespace BackendEjemplo.ManyToManyExample.Mapping
{
    public static class StudentMappings
    {
        public static StudentResource ToResource(this Student s) => new()
        {
            Id = s.Id,
            FirstName = s.FirstName,
            LastName = s.LastName,
            Email = s.Email
        };

        public static Student ToEntity(this SaveStudentResource r) => new()
        {
            FirstName = r.FirstName,
            LastName = r.LastName,
            Email = r.Email
        };
    }
}
