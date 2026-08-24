using BackendEjemplo.OneToManyExample.Domain.Models;
using BackendEjemplo.OneToManyExample.Domain.Repositories;
using BackendEjemplo.Shared.Domain.Repositories;
using BackendEjemplo.Shared.Persistence.Context;
using BackendEjemplo.Shared.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BackendEjemplo.OneToManyExample.Persistence.Repositories
{
    public class OrderRepository(AppDbContext context) : BaseRepository<Order>(context), IOrderRepository
    {
        public async Task<Order?> FindByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _context.Orders.Include(p => p.Client).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public override async Task<Page<Order>> ListPageAsync(
            int pageIndex,
            int pageSize,
            Expression<Func<Order, bool>>? filter = null,
            Func<IQueryable<Order>, IOrderedQueryable<Order>>? orderBy = null,
            CancellationToken cancellationToken = default
            )
        {
            if (pageIndex < 0) pageIndex = 0;

            if (pageSize < 1) pageSize = 10;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var query = GetQuery(filter, orderBy);

            var totalRecords = await query.CountAsync(cancellationToken);

            var data = await query
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .Include(p => p.Client)
                .ToListAsync(cancellationToken);

            return new Page<Order>
            {
                Data = data,
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalRecords = totalRecords
            };
        }
    }
}
