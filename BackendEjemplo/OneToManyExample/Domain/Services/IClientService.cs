using BackendEjemplo.OneToManyExample.Domain.Models;
using BackendEjemplo.OneToManyExample.Domain.Services.Communication;
using BackendEjemplo.Shared.Domain.Repositories;
using BackendEjemplo.Shared.Domain.Services.Communication;

namespace BackendEjemplo.OneToManyExample.Domain.Services
{
    public interface IClientService
    {
        Task<Page<Client>> ListPageAsync(ClientPageRequest request, CancellationToken cancellationToken = default);
        Task<BaseResponse<Client>> FindByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<BaseResponse<Client>> AddAsync(Client client, CancellationToken cancellationToken = default);
        Task<BaseResponse<Client>> UpdateAsync(long id, Client client, CancellationToken cancellationToken = default);
        Task<BaseResponse<Client>> DeleteAsync(long id, CancellationToken cancellationToken = default);
    }
}
