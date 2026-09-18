namespace BackendEjemplo.Shared.Extensions
{
    // Única fuente de verdad para la zona horaria de negocio ("America/Lima", sin horario
    // de verano — offset fijo UTC-5 todo el año). Usar BusinessClock.Now/Today en vez de
    // DateTime.Now/DateOnly.FromDateTime(DateTime.Now) en cualquier lugar donde "ahora"
    // deba reflejar la hora de Perú: el servidor (Azure App Service, East US 2) puede
    // estar en otro huso horario, y su hora local puede incluso caer en un día distinto
    // al de Perú justo alrededor de la medianoche.
    //
    // DateOnlyExtensions.ToStartOfBusinessDayUtc()/ToEndOfBusinessDayUtc() (usadas para
    // los filtros de rango de fecha, ver ARCHITECTURE.md sección 4) reutilizan TimeZone
    // en vez de tener su propia constante — esta es la única definición del huso horario
    // de negocio en todo el proyecto.
    public static class BusinessClock
    {
        public static readonly TimeZoneInfo TimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Lima");

        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZone);

        public static DateOnly Today => DateOnly.FromDateTime(Now);
    }
}
