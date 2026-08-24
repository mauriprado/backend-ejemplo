using BackendEjemplo.OneToManyExample.Domain.Enums;
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
    public class OrderServiceTests
    {
        private readonly Mock<IOrderRepository> _orderRepository = new();
        private readonly Mock<IClientRepository> _clientRepository = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly OrderService _sut;

        public OrderServiceTests()
        {
            _sut = new OrderService(_orderRepository.Object, _clientRepository.Object, _unitOfWork.Object);
        }

        private static Client SampleClient(long id = 1) => new() { Id = id, Name = "Ana", LastName = "Torres" };

        private static Order SampleOrder(long id = 1, long clientId = 1) => new()
        {
            Id = id,
            ClientId = clientId,
            OrderDate = DateTime.UtcNow,
            TotalAmount = 100,
            State = OrderState.Pending
        };

        [Fact]
        public async Task AddAsync_WhenClientDoesNotExist_DoesNotPersist()
        {
            _clientRepository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Client?)null);

            var result = await _sut.AddAsync(SampleOrder(clientId: 1), TestContext.Current.CancellationToken);

            // Sin esta validación, el insert reventaba con una violación de FK cruda
            // (500) en vez de un mensaje de negocio legible.
            result.Success.Should().BeFalse();
            _orderRepository.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AddAsync_WhenClientExists_PersistsAndReturnsSuccess()
        {
            _clientRepository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(SampleClient());
            var order = SampleOrder(clientId: 1);

            var result = await _sut.AddAsync(order, TestContext.Current.CancellationToken);

            result.Success.Should().BeTrue();
            _orderRepository.Verify(r => r.AddAsync(order, It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ChangeStateAsync_WhenNotFound_DoesNotPersist()
        {
            _orderRepository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

            var result = await _sut.ChangeStateAsync(1, OrderState.Paid, TestContext.Current.CancellationToken);

            result.Success.Should().BeFalse();
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ChangeStateAsync_WhenFound_UpdatesStateAndPersists()
        {
            var order = SampleOrder();
            _orderRepository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);

            var result = await _sut.ChangeStateAsync(1, OrderState.Paid, TestContext.Current.CancellationToken);

            result.Success.Should().BeTrue();
            order.State.Should().Be(OrderState.Paid);
            _orderRepository.Verify(r => r.Update(order), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ListPageAsync_WithNoFilters_MatchesEverything()
        {
            var getFilter = _orderRepository.CaptureListPageFilter<IOrderRepository, Order>(RepositoryMockExtensions.EmptyPage<Order>());

            await _sut.ListPageAsync(new OrderPageRequest(), TestContext.Current.CancellationToken);

            var filter = getFilter()!.Compile();
            filter(SampleOrder()).Should().BeTrue();
        }

        [Fact]
        public async Task ListPageAsync_FiltersByState()
        {
            var getFilter = _orderRepository.CaptureListPageFilter<IOrderRepository, Order>(RepositoryMockExtensions.EmptyPage<Order>());

            await _sut.ListPageAsync(new OrderPageRequest { OrderState = OrderState.Sent }, TestContext.Current.CancellationToken);

            var filter = getFilter()!.Compile();
            var sentOrder = SampleOrder();
            sentOrder.State = OrderState.Sent;
            filter(sentOrder).Should().BeTrue();
            filter(SampleOrder()).Should().BeFalse(); // Pending por defecto
        }

        [Fact]
        public async Task ListPageAsync_WithNoSortBy_KeepsHistoricalDefaultOrderByDateDescending()
        {
            // OrderService ya tenía un orden por defecto explícito (más reciente primero)
            // antes de que existiera el sorting pedido por el cliente — verifica que
            // agregar la whitelist de SortBy no rompió ese comportamiento histórico.
            var getOrderBy = _orderRepository.CaptureListPageOrderBy<IOrderRepository, Order>(RepositoryMockExtensions.EmptyPage<Order>());

            await _sut.ListPageAsync(new OrderPageRequest(), TestContext.Current.CancellationToken);

            var older = SampleOrder(1);
            older.OrderDate = DateTime.UtcNow.AddDays(-1);
            var newer = SampleOrder(2);
            newer.OrderDate = DateTime.UtcNow;
            getOrderBy([older, newer]).Select(o => o.Id).Should().Equal(2, 1);
        }

        [Fact]
        public async Task ListPageAsync_WithSortByTotalAmountAscending_OverridesDefaultOrder()
        {
            var getOrderBy = _orderRepository.CaptureListPageOrderBy<IOrderRepository, Order>(RepositoryMockExtensions.EmptyPage<Order>());

            await _sut.ListPageAsync(new OrderPageRequest { SortBy = "totalAmount" }, TestContext.Current.CancellationToken);

            var expensive = SampleOrder(1);
            expensive.TotalAmount = 500;
            var cheap = SampleOrder(2);
            cheap.TotalAmount = 50;
            getOrderBy([expensive, cheap]).Select(o => o.Id).Should().Equal(2, 1);
        }

        [Fact]
        public async Task ListPageAsync_FiltersByClientFullName_MatchesLastNamePartialAndCaseInsensitive()
        {
            var getFilter = _orderRepository.CaptureListPageFilter<IOrderRepository, Order>(RepositoryMockExtensions.EmptyPage<Order>());

            // "TORR" en mayúsculas y parcial: debe matchear "Torres" igual.
            await _sut.ListPageAsync(new OrderPageRequest { ClientFullName = "TORR" }, TestContext.Current.CancellationToken);

            var filter = getFilter()!.Compile();
            var order = SampleOrder();
            order.Client = SampleClient(); // LastName = "Torres"
            filter(order).Should().BeTrue();

            var otherOrder = SampleOrder();
            otherOrder.Client = new Client { Id = 2, Name = "Pedro", LastName = "Gomez" };
            filter(otherOrder).Should().BeFalse();
        }

        [Fact]
        public async Task ListPageAsync_FiltersByClientFullName_MatchesCombinedNameAndLastName()
        {
            // Regresión directa del bug real: antes se armaba con interpolación de string
            // ($"{a} {b}"), que compila a string.Format(...) y no lo puede traducir el
            // proveedor de Npgsql (InvalidOperationException en runtime, no en build).
            // Este test, además de validar el filtro, sirve como red de seguridad para
            // no reintroducir la interpolación por accidente.
            var getFilter = _orderRepository.CaptureListPageFilter<IOrderRepository, Order>(RepositoryMockExtensions.EmptyPage<Order>());

            await _sut.ListPageAsync(new OrderPageRequest { ClientFullName = "ana torres" }, TestContext.Current.CancellationToken);

            var filter = getFilter()!.Compile();
            var order = SampleOrder();
            order.Client = SampleClient(); // Name = "Ana", LastName = "Torres"
            filter(order).Should().BeTrue();

            // "torres ana" (orden invertido) no debe matchear: el filtro busca
            // "nombre apellido" en ese orden, no cualquier combinación.
            var otherOrder = SampleOrder();
            otherOrder.Client = SampleClient();
            var filterFlipped = new OrderPageRequest { ClientFullName = "torres ana" };
            var flippedFilter = _orderRepository.CaptureListPageFilter<IOrderRepository, Order>(RepositoryMockExtensions.EmptyPage<Order>());
            await _sut.ListPageAsync(filterFlipped, TestContext.Current.CancellationToken);
            flippedFilter()!.Compile()(otherOrder).Should().BeFalse();
        }
    }
}
