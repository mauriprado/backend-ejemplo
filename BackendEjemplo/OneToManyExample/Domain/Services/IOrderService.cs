using BackendEjemplo.OneToManyExample.Domain.Enums;
using BackendEjemplo.OneToManyExample.Domain.Models;
using BackendEjemplo.OneToManyExample.Domain.Services.Communication;
using BackendEjemplo.Shared.Domain.Repositories;
using BackendEjemplo.Shared.Domain.Services.Communication;

namespace BackendEjemplo.OneToManyExample.Domain.Services
{
    public interface IOrderService
    {
        Task<Page<Order>> ListPageAsync(OrderPageRequest request, CancellationToken cancellationToken = default);
        Task<BaseResponse<Order>> AddAsync(Order order, CancellationToken cancellationToken = default);
        Task<BaseResponse<Order>> ChangeStateAsync(long id, OrderState state, CancellationToken cancellationToken = default);
    }
}
