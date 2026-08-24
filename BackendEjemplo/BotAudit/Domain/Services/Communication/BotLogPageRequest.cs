using BackendEjemplo.Shared.Domain.Services.Communication;

namespace BackendEjemplo.BotAudit.Domain.Services.Communication
{
    public class BotLogPageRequest: BasePageRequest
    {
        public string? Bot {  get; set; }
        public string? Server { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string? Mensaje { get; set; }
        public bool? Falla { get; set; }
    }
}
