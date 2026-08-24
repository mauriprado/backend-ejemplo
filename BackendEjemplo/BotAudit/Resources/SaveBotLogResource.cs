using System.ComponentModel.DataAnnotations;

namespace BackendEjemplo.BotAudit.Resources
{
    public class SaveBotLogResource
    {
        [Required]
        [MaxLength(100)]
        public string Bot { get; set; }
        [Required]
        [MaxLength(100)]
        public string Server { get; set; }
        [Required]
        [MaxLength(100)]
        public string Subflujo { get; set; }
        // Nullable a propósito, igual que SaveEmployeeResource.HireDate (ver
        // ARCHITECTURE.md sección 7): [Required] sobre un value type no-nullable
        // nunca dispara, porque el binding nunca lo deja en null.
        [Required]
        public DateTime? Fecha { get; set; }
        [Required]
        [MaxLength(100)]
        public string UsuarioBot { get; set; }
        [Required]
        [MaxLength(100)]
        public string Plataforma { get; set; }
        [Required]
        [MaxLength(100)]
        public string UsuarioPlataforma { get; set; }
        [MaxLength(20)]
        public string? TipoDocumento { get; set; }
        [MaxLength(100)]
        public string? NroDocumento { get; set; }
        [Required]
        public string Mensaje { get; set; }
        [Required]
        public bool? Falla { get; set; }
    }
}
