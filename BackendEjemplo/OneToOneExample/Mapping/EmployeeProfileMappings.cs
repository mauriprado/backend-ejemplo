using BackendEjemplo.OneToOneExample.Domain.Models;
using BackendEjemplo.OneToOneExample.Resources;

namespace BackendEjemplo.OneToOneExample.Mapping
{
    public static class EmployeeProfileMappings
    {
        public static EmployeeProfileResource ToResource(this EmployeeProfile p) => new()
        {
            Id = p.Id,
            Biography = p.Biography,
            Address = p.Address,
            PhoneNumber = p.PhoneNumber,
            BirthDate = p.BirthDate,
            Employee = p.Employee.ToResource()
        };

        public static EmployeeProfile ToEntity(this SaveEmployeeProfileResource r) => new()
        {
            Biography = r.Biography,
            Address = r.Address,
            PhoneNumber = r.PhoneNumber,
            BirthDate = r.BirthDate!.Value,
            EmployeeId = r.EmployeeId!.Value
        };
    }
}
