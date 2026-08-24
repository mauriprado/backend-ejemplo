using BackendEjemplo.BotAudit.Domain.Models;
using BackendEjemplo.BotAudit.Domain.Services.Communication;
using BackendEjemplo.Shared.Domain.Repositories;
using BackendEjemplo.Shared.Domain.Services.Communication;

namespace BackendEjemplo.BotAudit.Domain.Services
{
    public interface IBotLogService
    {
        Task<Page<BotLog>> ListPageAsync(BotLogPageRequest request, CancellationToken cancellationToken = default);
        Task<BaseResponse<BotLog>> FindByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<BaseResponse<BotLog>> AddAsync(BotLog botLog, CancellationToken cancellationToken = default);
        Task<BaseResponse<BotLog>> UpdateAsync(long id, BotLog botLog, CancellationToken cancellationToken = default);
        Task<BaseResponse<BotLog>> DeleteAsync(long id, CancellationToken cancellationToken = default);
    }
}
