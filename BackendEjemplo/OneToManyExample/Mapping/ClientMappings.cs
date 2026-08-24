using BackendEjemplo.OneToManyExample.Domain.Models;
using BackendEjemplo.OneToManyExample.Resources;

namespace BackendEjemplo.OneToManyExample.Mapping
{
    public static class ClientMappings
    {
        public static ClientResource ToResource(this Client c) => new()
        {
            Id = c.Id,
            Name = c.Name,
            LastName = c.LastName,
            Email = c.Email,
            PhoneNumber = c.PhoneNumber,
            RegistrationDate = c.RegistrationDate
        };

        public static Client ToEntity(this SaveClientResource r) => new()
        {
            Name = r.Name,
            LastName = r.LastName,
            Email = r.Email,
            PhoneNumber = r.PhoneNumber,
            RegistrationDate = DateTime.UtcNow
        };
    }
}
