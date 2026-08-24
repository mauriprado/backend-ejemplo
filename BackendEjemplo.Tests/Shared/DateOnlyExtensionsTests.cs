using BackendEjemplo.Shared.Extensions;
using AwesomeAssertions;

namespace BackendEjemplo.Tests.Shared
{
    public class DateOnlyExtensionsTests
    {
        [Fact]
        public void ToStartOfBusinessDayUtc_ConvertsMidnightLimaToFiveAmUtc()
        {
            // America/Lima es UTC-5 todo el año (sin horario de verano), así que
            // "00:00 del día X en Lima" siempre es "05:00 UTC del mismo día X".
            var date = new DateOnly(2026, 8, 7);

            date.ToStartOfBusinessDayUtc().Should().Be(new DateTime(2026, 8, 7, 5, 0, 0, DateTimeKind.Utc));
        }

        [Fact]
        public void ToEndOfBusinessDayUtc_ConvertsEndOfLimaDayToNextDayFiveAmUtc()
        {
            // "23:59:59.9999999 del día X en Lima" cae en la madrugada del día X+1 en UTC.
            var date = new DateOnly(2026, 8, 7);

            var result = date.ToEndOfBusinessDayUtc();

            result.Date.Should().Be(new DateTime(2026, 8, 8));
            result.Hour.Should().Be(4);
            result.Minute.Should().Be(59);
            result.Kind.Should().Be(DateTimeKind.Utc);
        }

        [Fact]
        public void BusinessDayBoundaries_CorrectlyClassifyARecordCreatedLateAtNightInLima()
        {
            // Caso concreto que motivó este helper: un registro creado a las 20:00 hora
            // Lima del día 6 queda guardado como 2026-08-07T01:00:00Z (mismo instante,
            // ya cruzó medianoche en UTC). Antes de este fix, filtrar por
            // startDate=2026-08-06&endDate=2026-08-06 NO lo encontraba (quedaba
            // "escondido" bajo el día 7 en UTC) — con el helper, sí.
            var recordUtc = new DateTime(2026, 8, 7, 1, 0, 0, DateTimeKind.Utc); // 20:00 Lima del día 6
            var day6 = new DateOnly(2026, 8, 6);
            var day7 = new DateOnly(2026, 8, 7);

            (recordUtc >= day6.ToStartOfBusinessDayUtc() && recordUtc <= day6.ToEndOfBusinessDayUtc())
                .Should().BeTrue("el registro fue creado un 6 de agosto en hora Lima");

            (recordUtc >= day7.ToStartOfBusinessDayUtc() && recordUtc <= day7.ToEndOfBusinessDayUtc())
                .Should().BeFalse("no debería contar como parte del día 7 en Lima");
        }
    }
}
