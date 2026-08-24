namespace BackendEjemplo.Shared.Extensions
{
    // Todo filtro de rango de fecha (startDate/endDate) en un PageRequest usa DateOnly,
    // no DateTime — el cliente no debería tener que inventar una hora para expresar
    // "desde tal día hasta tal día" (ver ARCHITECTURE.md sección 4, "Zona horaria en
    // filtros de fecha"). Pero las columnas contra las que se compara son DateTime/UTC
    // (timestamp with time zone en Postgres), así que hay que decidir A QUÉ zona horaria
    // pertenece "el día" antes de convertir a los límites UTC del rango.
    //
    // BusinessTimeZone es la única línea que hay que tocar si la empresa opera desde otro
    // huso horario. "America/Lima" no tiene horario de verano (offset fijo todo el año),
    // así que acá no hace falta lidiar con ambigüedad de DST — en un huso que sí lo tenga,
    // TimeZoneInfo.ConvertTimeToUtc ya resuelve el offset correcto para cada fecha.
    public static class DateOnlyExtensions
    {
        private static readonly TimeZoneInfo BusinessTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Lima");

        // Instante UTC que corresponde al inicio (00:00:00) de ese día calendario en la
        // zona horaria de negocio. Usar como límite inferior (>=) de un filtro de rango.
        public static DateTime ToStartOfBusinessDayUtc(this DateOnly date) =>
            TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MinValue), BusinessTimeZone);

        // Instante UTC que corresponde al final (23:59:59.9999999) de ese día calendario
        // en la zona horaria de negocio. Usar como límite superior (<=) de un filtro de rango.
        public static DateTime ToEndOfBusinessDayUtc(this DateOnly date) =>
            TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MaxValue), BusinessTimeZone);
    }
}
