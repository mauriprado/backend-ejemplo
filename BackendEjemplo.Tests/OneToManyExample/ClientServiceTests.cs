using BackendEjemplo.OneToManyExample.Domain.Models;
using BackendEjemplo.OneToManyExample.Domain.Repositories;
using BackendEjemplo.OneToManyExample.Domain.Services.Communication;
using BackendEjemplo.OneToManyExample.Services;
using BackendEjemplo.Shared.Domain.Repositories;
using BackendEjemplo.Tests.TestHelpers;
using AwesomeAssertions;
using Moq;

namespace BackendEjemplo.Tests.OneToManyExample
{
    public class ClientServiceTests
    {
        private readonly Mock<IClientRepository> _clientRepository = new();
        private readonly Mock<IOrderRepository> _orderRepository = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly ClientService _sut;

        public ClientServiceTests()
        {
            _sut = new ClientService(_clientRepository.Object, _orderRepository.Object, _unitOfWork.Object);
        }

        private static Client SampleClient(long id = 1) => new()
        {
            Id = id,
            Name = "Ana",
            LastName = "Torres",
            Email = "ana@test.com",
            PhoneNumber = "999999999",
            RegistrationDate = DateTime.UtcNow
        };

        [Fact]
        public async Task AddAsync_PersistsAndReturnsSuccess()
        {
            var client = SampleClient();

            var result = await _sut.AddAsync(client, TestContext.Current.CancellationToken);

            result.Success.Should().BeTrue();
            _clientRepository.Verify(r => r.AddAsync(client, It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenClientDoesNotExist_ReturnsNotFoundWithoutConflict()
        {
            _clientRepository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Client?)null);

            var result = await _sut.DeleteAsync(1, TestContext.Current.CancellationToken);

            result.Success.Should().BeFalse();
            result.IsConflict.Should().BeFalse();
            _clientRepository.Verify(r => r.Remove(It.IsAny<Client>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenClientHasOrders_ReturnsConflictAndDoesNotDelete()
        {
            var client = SampleClient();
            _clientRepository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(client);
            _orderRepository.Setup(r => r.ListPageAsync(0, 1, It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Page<Order> { Data = [new Order()], PageIndex = 0, PageSize = 1, TotalRecords = 1 });

            var result = await _sut.DeleteAsync(1, TestContext.Current.CancellationToken);

            // Este es el caso que, sin el pre-check, terminaba como un 500 crudo por la
            // violación de la FK Restrict en la base — acá se verifica que el Service
            // lo intercepta ANTES de llegar a la base y lo marca como conflicto (409).
            result.Success.Should().BeFalse();
            result.IsConflict.Should().BeTrue();
            _clientRepository.Verify(r => r.Remove(It.IsAny<Client>()), Times.Never);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenClientHasNoOrders_RemovesAndPersists()
        {
            var client = SampleClient();
            _clientRepository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(client);
            _orderRepository.Setup(r => r.ListPageAsync(0, 1, It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(RepositoryMockExtensions.EmptyPage<Order>());

            var result = await _sut.DeleteAsync(1, TestContext.Current.CancellationToken);

            result.Success.Should().BeTrue();
            result.IsConflict.Should().BeFalse();
            _clientRepository.Verify(r => r.Remove(client), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ListPageAsync_WithNoFilters_MatchesEverything()
        {
            var getFilter = _clientRepository.CaptureListPageFilter<IClientRepository, Client>(RepositoryMockExtensions.EmptyPage<Client>());

            await _sut.ListPageAsync(new ClientPageRequest(), TestContext.Current.CancellationToken);

            var filter = getFilter()!.Compile();
            filter(SampleClient()).Should().BeTrue();
        }

        [Fact]
        public async Task ListPageAsync_FiltersByFullName_MatchesNameOrLastNamePartialAndCaseInsensitive()
        {
            var getFilter = _clientRepository.CaptureListPageFilter<IClientRepository, Client>(RepositoryMockExtensions.EmptyPage<Client>());

            // "TORR" en mayúsculas y parcial: debe matchear "Torres" igual.
            await _sut.ListPageAsync(new ClientPageRequest { FullName = "TORR" }, TestContext.Current.CancellationToken);

            var filter = getFilter()!.Compile();
            filter(SampleClient()).Should().BeTrue(); // matchea por LastName

            var other = SampleClient();
            other.Name = "Pedro";
            other.LastName = "Gomez";
            filter(other).Should().BeFalse();
        }

        [Fact]
        public async Task ListPageAsync_FiltersByFullName_MatchesCombinedNameAndLastName()
        {
            // Regresión directa del bug real encontrado en OrderService.ClientFullName
            // (ver ARCHITECTURE.md sección 7): interpolar $"{a} {b}" sobre columnas de la
            // entidad compila a string.Format(...), que Npgsql no traduce. Acá se arma con
            // concatenación (+), que sí traduce — este test también sirve de red de
            // seguridad para no reintroducir la interpolación por accidente.
            var getFilter = _clientRepository.CaptureListPageFilter<IClientRepository, Client>(RepositoryMockExtensions.EmptyPage<Client>());

            await _sut.ListPageAsync(new ClientPageRequest { FullName = "ana torres" }, TestContext.Current.CancellationToken);

            var filter = getFilter()!.Compile();
            filter(SampleClient()).Should().BeTrue(); // Name = "Ana", LastName = "Torres"

            // "torres ana" (orden invertido) no debe matchear: el filtro busca
            // "nombre apellido" en ese orden, no cualquier combinación.
            var getFlippedFilter = _clientRepository.CaptureListPageFilter<IClientRepository, Client>(RepositoryMockExtensions.EmptyPage<Client>());
            await _sut.ListPageAsync(new ClientPageRequest { FullName = "torres ana" }, TestContext.Current.CancellationToken);
            getFlippedFilter()!.Compile()(SampleClient()).Should().BeFalse();
        }

        [Fact]
        public async Task ListPageAsync_WithNoSortBy_DefaultsToOrderById()
        {
            var getOrderBy = _clientRepository.CaptureListPageOrderBy<IClientRepository, Client>(RepositoryMockExtensions.EmptyPage<Client>());

            await _sut.ListPageAsync(new ClientPageRequest(), TestContext.Current.CancellationToken);

            var clients = new[] { SampleClient(3), SampleClient(1), SampleClient(2) };
            getOrderBy(clients).Select(c => c.Id).Should().Equal(1, 2, 3);
        }

        [Fact]
        public async Task ListPageAsync_WithSortByNameDescending_AppliesRequestedOrder()
        {
            var getOrderBy = _clientRepository.CaptureListPageOrderBy<IClientRepository, Client>(RepositoryMockExtensions.EmptyPage<Client>());

            await _sut.ListPageAsync(new ClientPageRequest { SortBy = "name", SortDescending = true }, TestContext.Current.CancellationToken);

            var ana = SampleClient(1);
            ana.Name = "Ana";
            var beto = SampleClient(2);
            beto.Name = "Beto";
            getOrderBy([ana, beto]).Select(c => c.Name).Should().Equal("Beto", "Ana");
        }

        [Fact]
        public async Task ListPageAsync_WithUnknownSortBy_FallsBackToDefaultOrder()
        {
            var getOrderBy = _clientRepository.CaptureListPageOrderBy<IClientRepository, Client>(RepositoryMockExtensions.EmptyPage<Client>());

            // "apellido" no existe en la whitelist de ClientService (es "lastName"): debe
            // ignorarse en silencio y caer al orden por defecto (por Id), no romper la query.
            await _sut.ListPageAsync(new ClientPageRequest { SortBy = "apellido" }, TestContext.Current.CancellationToken);

            var clients = new[] { SampleClient(2), SampleClient(1) };
            getOrderBy(clients).Select(c => c.Id).Should().Equal(1, 2);
        }

        [Fact]
        public async Task ListPageAsync_FiltersByRegistrationDateRange()
        {
            var getFilter = _clientRepository.CaptureListPageFilter<IClientRepository, Client>(RepositoryMockExtensions.EmptyPage<Client>());

            var now = DateTime.UtcNow;
            var today = DateOnly.FromDateTime(now);
            await _sut.ListPageAsync(new ClientPageRequest
            {
                StartRegistrationDate = today.AddDays(-1),
                EndRegistrationDate = today.AddDays(1)
            }, TestContext.Current.CancellationToken);

            var filter = getFilter()!.Compile();
            var inRange = SampleClient();
            inRange.RegistrationDate = now;
            filter(inRange).Should().BeTrue();

            var outOfRange = SampleClient();
            outOfRange.RegistrationDate = now.AddDays(-10);
            filter(outOfRange).Should().BeFalse();
        }
    }
}
