using BackendEjemplo.OneToManyExample.Domain.Enums;
using BackendEjemplo.OneToManyExample.Domain.Models;
using BackendEjemplo.OneToManyExample.Domain.Repositories;
using BackendEjemplo.OneToManyExample.Domain.Services;
using BackendEjemplo.OneToManyExample.Domain.Services.Communication;
using BackendEjemplo.Shared.Domain.Repositories;
using BackendEjemplo.Shared.Domain.Services.Communication;
using BackendEjemplo.Shared.Extensions;
using System.Linq.Expressions;

namespace BackendEjemplo.OneToManyExample.Services
{
    public class OrderService(
        IOrderRepository orderRepository,
        IClientRepository clientRepository,
        IUnitOfWork unitOfWork) : IOrderService
    {
        private static readonly Dictionary<string, Expression<Func<Order, object>>> SortableColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = o => o.Id,
            ["orderDate"] = o => o.OrderDate,
            ["totalAmount"] = o => o.TotalAmount,
            ["state"] = o => o.State
        };

        public async Task<BaseResponse<Order>> AddAsync(Order order, CancellationToken cancellationToken = default)
        {
            var existingClient = await clientRepository.FindByIdAsync(order.ClientId, cancellationToken);

            if (existingClient is null) return new BaseResponse<Order>($"Cliente con id {order.ClientId} no existe");

            await orderRepository.AddAsync(order, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            return new BaseResponse<Order>(order);
        }

        public async Task<BaseResponse<Order>> ChangeStateAsync(long id, OrderState state, CancellationToken cancellationToken = default)
        {
            var existingOrder = await orderRepository.FindByIdAsync(id, cancellationToken);

            if (existingOrder is null) return new BaseResponse<Order>($"Orden con id {id} no existe");

            existingOrder.State = state;

            orderRepository.Update(existingOrder);
            await unitOfWork.CompleteAsync(cancellationToken);

            return new BaseResponse<Order>(existingOrder);
        }

        public async Task<Page<Order>> ListPageAsync(OrderPageRequest request, CancellationToken cancellationToken = default)
        {
            Expression<Func<Order, bool>>? filter;

            // clientSearchTerm se arma sobre una variable local (closure), no sobre
            // columnas de la entidad, así que EF Core lo evalúa en memoria antes de
            // traducir a SQL. Ahí terminaba el problema original: interpolar
            // directamente order.Client.Name/LastName (columnas de la entidad) dentro
            // del lambda compila a string.Format(...), que el proveedor de Npgsql no
            // puede traducir (InvalidOperationException en runtime, no en build).
            //
            // Nota: EF.Functions.ILike sería más idiomático en Postgres (traduce a ILIKE,
            // case-insensitive nativo), pero rompe el patrón de test de este proyecto
            // (CaptureListPageFilter + Compile(), sección 8.2 de ARCHITECTURE.md): su
            // implementación en CLR tira excepción a propósito si se ejecuta fuera de una
            // traducción a SQL. .ToLower()/Contains()/+ sí funcionan en ambos mundos.
            var clientSearchTerm = request.ClientFullName?.ToLower();

            filter = order =>
            (!request.StartOrderDate.HasValue || order.OrderDate >= request.StartOrderDate.Value.ToStartOfBusinessDayUtc()) &&
            (!request.EndOrderDate.HasValue || order.OrderDate <= request.EndOrderDate.Value.ToEndOfBusinessDayUtc()) &&
            (!request.OrderState.HasValue || order.State == request.OrderState.Value) &&
            (string.IsNullOrWhiteSpace(clientSearchTerm) ||
                order.Client.Name.ToLower().Contains(clientSearchTerm) ||
                order.Client.LastName.ToLower().Contains(clientSearchTerm) ||
                (order.Client.Name.ToLower() + " " + order.Client.LastName.ToLower()).Contains(clientSearchTerm));

            var page = await orderRepository.ListPageAsync(
                request.PageIndex,
                request.PageSize,
                filter,
                // Por defecto (sin sortBy explícito) se mantiene el orden histórico:
                // pedidos más recientes primero.
                orderBy: q => q.ApplySort(request.SortBy, request.SortDescending, SortableColumns, defaultColumn: o => o.OrderDate, defaultDescending: true),
                cancellationToken: cancellationToken
                );

            return page;
        }
    }
}
