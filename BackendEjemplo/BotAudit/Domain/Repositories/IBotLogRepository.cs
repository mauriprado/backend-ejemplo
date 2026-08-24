using BackendEjemplo.BotAudit.Domain.Models;
using BackendEjemplo.Shared.Domain.Repositories;

namespace BackendEjemplo.BotAudit.Domain.Repositories
{
    public interface IBotLogRepository: IBaseRepository<BotLog>
    {
        Task<BotLog?> FindByIdAsync(long id, CancellationToken cancellationToken = default);
    }
}
