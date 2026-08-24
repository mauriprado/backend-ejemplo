using BackendEjemplo.BotAudit.Domain.Models;
using BackendEjemplo.BotAudit.Domain.Repositories;
using BackendEjemplo.BotAudit.Domain.Services.Communication;
using BackendEjemplo.BotAudit.Services;
using BackendEjemplo.Shared.Domain.Repositories;
using BackendEjemplo.Tests.TestHelpers;
using AwesomeAssertions;
using Moq;

namespace BackendEjemplo.Tests.BotAudit
{
    public class BotLogServiceTests
    {
        private readonly Mock<IBotLogRepository> _repository = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly BotLogService _sut;

        public BotLogServiceTests()
        {
            _sut = new BotLogService(_repository.Object, _unitOfWork.Object);
        }

        private static BotLog SampleLog(long id = 1) => new()
        {
            Id = id,
            Bot = "BotVentas",
            Server = "srv-01",
            Subflujo = "consulta",
            Fecha = DateTime.UtcNow,
            UsuarioBot = "usr-bot",
            Plataforma = "WhatsApp",
            UsuarioPlataforma = "51999999999",
            Mensaje = "hola",
            Falla = false
        };

        [Fact]
        public async Task AddAsync_PersistsAndReturnsSuccess()
        {
            var log = SampleLog();

            var result = await _sut.AddAsync(log, TestContext.Current.CancellationToken);

            result.Success.Should().BeTrue();
            result.Content.Should().Be(log);
            _repository.Verify(r => r.AddAsync(log, It.IsAny<CancellationToken>()), Times.Once);
            // Sin este await, la request devolvería 200/201 pero no persistiría nada
            // (bug real que se coló en este proyecto en Update/Delete).
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task FindByIdAsync_WhenNotFound_ReturnsFailureWithout404Leak()
        {
            _repository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((BotLog?)null);

            var result = await _sut.FindByIdAsync(1, TestContext.Current.CancellationToken);

            result.Success.Should().BeFalse();
            result.IsConflict.Should().BeFalse();
            result.Message.Should().Contain("1");
        }

        [Fact]
        public async Task FindByIdAsync_WhenFound_ReturnsSuccess()
        {
            var log = SampleLog();
            _repository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(log);

            var result = await _sut.FindByIdAsync(1, TestContext.Current.CancellationToken);

            result.Success.Should().BeTrue();
            result.Content.Should().Be(log);
        }

        [Fact]
        public async Task UpdateAsync_WhenNotFound_DoesNotPersist()
        {
            _repository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((BotLog?)null);

            var result = await _sut.UpdateAsync(1, SampleLog(), TestContext.Current.CancellationToken);

            result.Success.Should().BeFalse();
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenFound_CopiesFieldsAndPersists()
        {
            var existing = SampleLog();
            _repository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

            var updated = SampleLog();
            updated.Bot = "OtroBot";
            updated.Mensaje = "chau";

            var result = await _sut.UpdateAsync(1, updated, TestContext.Current.CancellationToken);

            result.Success.Should().BeTrue();
            existing.Bot.Should().Be("OtroBot");
            existing.Mensaje.Should().Be("chau");
            _repository.Verify(r => r.Update(existing), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenNotFound_DoesNotRemoveOrPersist()
        {
            _repository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((BotLog?)null);

            var result = await _sut.DeleteAsync(1, TestContext.Current.CancellationToken);

            result.Success.Should().BeFalse();
            _repository.Verify(r => r.Remove(It.IsAny<BotLog>()), Times.Never);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenFound_RemovesAndPersists()
        {
            var existing = SampleLog();
            _repository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

            var result = await _sut.DeleteAsync(1, TestContext.Current.CancellationToken);

            result.Success.Should().BeTrue();
            _repository.Verify(r => r.Remove(existing), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ListPageAsync_WithNoFilters_MatchesEverything()
        {
            var getFilter = _repository.CaptureListPageFilter<IBotLogRepository, BotLog>(RepositoryMockExtensions.EmptyPage<BotLog>());

            await _sut.ListPageAsync(new BotLogPageRequest(), TestContext.Current.CancellationToken);

            var filter = getFilter()!.Compile();
            // Regresión directa del bug "!IsNullOrWhiteSpace(x) || Contains(x)": sin
            // ningún filtro seteado en el request, CUALQUIER fila debe matchear.
            filter(SampleLog()).Should().BeTrue();
        }

        [Fact]
        public async Task ListPageAsync_FiltersByBotName()
        {
            var getFilter = _repository.CaptureListPageFilter<IBotLogRepository, BotLog>(RepositoryMockExtensions.EmptyPage<BotLog>());

            await _sut.ListPageAsync(new BotLogPageRequest { Bot = "Ventas" }, TestContext.Current.CancellationToken);

            var filter = getFilter()!.Compile();
            filter(SampleLog()).Should().BeTrue(); // Bot = "BotVentas" contiene "Ventas"

            var other = SampleLog();
            other.Bot = "BotSoporte";
            filter(other).Should().BeFalse();
        }

        [Fact]
        public async Task ListPageAsync_FiltersByFalla()
        {
            var getFilter = _repository.CaptureListPageFilter<IBotLogRepository, BotLog>(RepositoryMockExtensions.EmptyPage<BotLog>());

            await _sut.ListPageAsync(new BotLogPageRequest { Falla = true }, TestContext.Current.CancellationToken);

            var filter = getFilter()!.Compile();
            var failedLog = SampleLog();
            failedLog.Falla = true;
            filter(failedLog).Should().BeTrue();
            filter(SampleLog()).Should().BeFalse(); // Falla = false por defecto en SampleLog()
        }

        [Fact]
        public async Task ListPageAsync_FiltersByFalla_EvenWithoutStartDate()
        {
            // Regresión: falta un ")" después de StartDate.Value.ToDateTime(TimeOnly.MinValue)
            // en el filtro. Por precedencia de operadores, eso mete el chequeo de EndDate/
            // Mensaje/Falla DENTRO del "|| " de StartDate — si no se manda StartDate
            // (el caso normal), esos tres filtros quedan anulados en silencio.
            var getFilter = _repository.CaptureListPageFilter<IBotLogRepository, BotLog>(RepositoryMockExtensions.EmptyPage<BotLog>());

            await _sut.ListPageAsync(new BotLogPageRequest { Falla = true }, TestContext.Current.CancellationToken);

            var filter = getFilter()!.Compile();
            filter(SampleLog()).Should().BeFalse(); // Falla = false, no debería matchear Falla = true
        }

        [Fact]
        public async Task ListPageAsync_FiltersByDateRange_UsingWholeDaysFromDateOnly()
        {
            var getFilter = _repository.CaptureListPageFilter<IBotLogRepository, BotLog>(RepositoryMockExtensions.EmptyPage<BotLog>());

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            await _sut.ListPageAsync(new BotLogPageRequest { StartDate = today, EndDate = today }, TestContext.Current.CancellationToken);

            var filter = getFilter()!.Compile();

            var logToday = SampleLog();
            logToday.Fecha = today.ToDateTime(TimeOnly.MinValue).AddHours(23); // dentro del día, aunque no sea medianoche
            filter(logToday).Should().BeTrue();

            var logYesterday = SampleLog();
            logYesterday.Fecha = today.AddDays(-1).ToDateTime(TimeOnly.MinValue);
            filter(logYesterday).Should().BeFalse();
        }
    }
}
