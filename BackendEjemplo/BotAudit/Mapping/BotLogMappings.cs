using BackendEjemplo.BotAudit.Domain.Models;
using BackendEjemplo.BotAudit.Resources;

namespace BackendEjemplo.BotAudit.Mapping
{
    public static class BotLogMappings
    {
        public static BotLog ToEntity(this SaveBotLogResource r) => new()
        {
            Bot = r.Bot,
            Server = r.Server,
            Subflujo = r.Subflujo,
            Fecha = r.Fecha!.Value,
            UsuarioBot = r.UsuarioBot,
            Plataforma = r.Plataforma,
            UsuarioPlataforma = r.UsuarioPlataforma,
            TipoDocumento = r.TipoDocumento,
            NroDocumento = r.NroDocumento,
            Mensaje = r.Mensaje,
            Falla = r.Falla!.Value
        };
    }
}
