using BackendEjemplo.OneToManyExample.Domain.Models;
using BackendEjemplo.OneToManyExample.Domain.Repositories;
using BackendEjemplo.Shared.Domain.Services.Communication;
using BackendEjemplo.Shared.Persistence.Context;
using BackendEjemplo.Shared.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BackendEjemplo.OneToManyExample.Persistence.Repositories
{
    public class ClientRepository(AppDbContext context) : BaseRepository<Client>(context), IClientRepository
    {
        public async Task<Client?> FindByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _context.Clients.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }
    }
}
