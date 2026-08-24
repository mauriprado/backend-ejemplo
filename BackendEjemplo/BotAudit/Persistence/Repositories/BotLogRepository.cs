using BackendEjemplo.BotAudit.Domain.Models;
using BackendEjemplo.BotAudit.Domain.Repositories;
using BackendEjemplo.Shared.Persistence.Context;
using BackendEjemplo.Shared.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BackendEjemplo.BotAudit.Persistence.Repositories
{
    public class BotLogRepository(AppDbContext context) : BaseRepository<BotLog>(context), IBotLogRepository
    {
        public async Task<BotLog?> FindByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _context.BotLogs.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }
    }
}
