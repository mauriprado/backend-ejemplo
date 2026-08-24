using BackendEjemplo.ManyToManyExample.Domain.Models;
using BackendEjemplo.ManyToManyExample.Domain.Repositories;
using BackendEjemplo.Shared.Domain.Repositories;
using BackendEjemplo.Shared.Persistence.Context;
using BackendEjemplo.Shared.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BackendEjemplo.ManyToManyExample.Persistence.Repositories
{
    public class EnrollmentRepository(AppDbContext context) : BaseRepository<Enrollment>(context), IEnrollmentRepository
    {
        public async Task<Enrollment?> FindByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _context.Enrollments
                .Include(p => p.Student)
                .Include(p => p.Course)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public override async Task<Page<Enrollment>> ListPageAsync(
            int pageIndex,
            int pageSize,
            Expression<Func<Enrollment, bool>>? filter = null,
            Func<IQueryable<Enrollment>, IOrderedQueryable<Enrollment>>? orderBy = null,
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
                .Include(p => p.Student)
                .Include(p => p.Course)
                .ToListAsync(cancellationToken);

            return new Page<Enrollment>
            {
                Data = data,
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalRecords = totalRecords
            };
        }
    }
}
