using System.ComponentModel.DataAnnotations;

namespace BackendEjemplo.OneToOneExample.Resources
{
    public class SaveEmployeeResource
    {
        [Required, MaxLength(100)]
        public string FirstName { get; set; }
        [Required, MaxLength(100)]
        public string LastName { get; set; }
        [Required, MaxLength(50)]
        public string Email { get; set; }
        [Required, MaxLength(100)]
        public string Position { get; set; }
        // Nullable a propósito: si fuera "DateOnly HireDate" (no nullable), un POST sin
        // hireDate en el body no deja la propiedad en null al deserializar — la deja en
        // default(DateOnly) (0001-01-01), y [Required] solo chequea "value == null", así
        // que nunca dispara para un value type no-nullable. Ver ARCHITECTURE.md sección 7.
        [Required]
        public DateOnly? HireDate { get; set; }
    }
}
