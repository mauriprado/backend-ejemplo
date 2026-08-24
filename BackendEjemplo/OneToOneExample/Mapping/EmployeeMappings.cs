using BackendEjemplo.OneToOneExample.Domain.Models;
using BackendEjemplo.OneToOneExample.Resources;

namespace BackendEjemplo.OneToOneExample.Mapping
{
    public static class EmployeeMappings
    {
        public static EmployeeResource ToResource(this Employee e) => new()
        {
            Id = e.Id,
            FirstName = e.FirstName,
            LastName = e.LastName,
            Email = e.Email,
            Position = e.Position,
            HireDate = e.HireDate
        };

        public static Employee ToEntity(this SaveEmployeeResource r) => new()
        {
            FirstName = r.FirstName,
            LastName = r.LastName,
            Email = r.Email,
            Position = r.Position,
            // .Value es seguro acá: el Controller ya devolvió 400 vía ValidationProblem
            // si HireDate llegó null (ModelState.IsValid lo captura por el [Required]
            // de arriba, ya que ahora es un DateOnly? y sí puede ser null de verdad).
            HireDate = r.HireDate!.Value
        };
    }
}
