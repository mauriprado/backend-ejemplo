namespace BackendEjemplo.BotAudit.Domain.Models
{
    public class BotLog
    {
        public long Id { get; set; }
        public string Bot {  get; set; }
        public string Server { get; set; }
        public string Subflujo { get; set; }
        public DateTime Fecha {  get; set; }
        public string UsuarioBot { get; set; }
        public string Plataforma { get; set; }
        public string UsuarioPlataforma { get; set; }
        public string? TipoDocumento { get; set; }
        public string? NroDocumento { get; set; }
        public string Mensaje { get; set; }
        public bool Falla {  get; set; }
    }
}
