using BackendEjemplo.OneToManyExample.Domain.Models;
using BackendEjemplo.Shared.Domain.Repositories;

namespace BackendEjemplo.OneToManyExample.Domain.Repositories
{
    public interface IOrderRepository: IBaseRepository<Order>
    {
        Task<Order?> FindByIdAsync(long id, CancellationToken cancellationToken = default);
    }
}
