using BackendEjemplo.OneToOneExample.Domain.Models;
using BackendEjemplo.OneToOneExample.Domain.Repositories;
using BackendEjemplo.Shared.Domain.Repositories;
using BackendEjemplo.Shared.Persistence.Context;
using BackendEjemplo.Shared.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BackendEjemplo.OneToOneExample.Persistence.Repositories
{
    public class EmployeeProfileRepository(AppDbContext context) : BaseRepository<EmployeeProfile>(context), IEmployeeProfileRepository
    {
        public async Task<EmployeeProfile?> FindByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _context.EmployeeProfiles.Include(p => p.Employee).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public override async Task<Page<EmployeeProfile>> ListPageAsync(
            int pageIndex,
            int pageSize,
            Expression<Func<EmployeeProfile, bool>>? filter = null,
            Func<IQueryable<EmployeeProfile>, IOrderedQueryable<EmployeeProfile>>? orderBy = null,
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
                .Include(p => p.Employee)
                .ToListAsync(cancellationToken);

            return new Page<EmployeeProfile>
            {
                Data = data,
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalRecords = totalRecords
            };
        }
    }
}
