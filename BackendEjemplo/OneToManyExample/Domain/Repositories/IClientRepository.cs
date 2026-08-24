using BackendEjemplo.OneToManyExample.Domain.Models;
using BackendEjemplo.Shared.Domain.Repositories;

namespace BackendEjemplo.OneToManyExample.Domain.Repositories
{
    public interface IClientRepository: IBaseRepository<Client>
    {
        Task<Client?> FindByIdAsync(long id, CancellationToken cancellationToken = default);
    }
}
