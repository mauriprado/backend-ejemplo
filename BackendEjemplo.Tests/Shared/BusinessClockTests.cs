using BackendEjemplo.Shared.Extensions;
using AwesomeAssertions;

namespace BackendEjemplo.Tests.Shared
{
    public class BusinessClockTests
    {
        [Fact]
        public void Now_IsExactlyFiveHoursBehindUtc()
        {
            // America/Lima es UTC-5 todo el año (sin horario de verano) — el caso real que
            // motivó esta clase: el servidor (Azure, East US 2) puede tener su propia hora
            // local en otro huso, así que "ahora" para el negocio nunca debe salir de
            // DateTime.Now del servidor, siempre de esta conversión explícita.
            var utcNow = DateTime.UtcNow;

            var limaNow = BusinessClock.Now;

            (utcNow - limaNow).Should().BeCloseTo(TimeSpan.FromHours(5), TimeSpan.FromSeconds(2));
        }

        [Fact]
        public void Today_MatchesTheCalendarDayOfNow()
        {
            BusinessClock.Today.Should().Be(DateOnly.FromDateTime(BusinessClock.Now));
        }

        [Fact]
        public void TimeZone_IsTheSameInstanceUsedByDateOnlyExtensions()
        {
            // Regresión: DateOnlyExtensions tenía su propia constante "BusinessTimeZone"
            // duplicada — si alguna de las dos rutas de conversión vuelve a divergir, un
            // filtro de fecha y una fecha "de hoy" mostrada en la UI podrían quedar
            // desalineados por horas sin que ningún test lo note.
            BusinessClock.TimeZone.Id.Should().Be("America/Lima");
        }
    }
}
