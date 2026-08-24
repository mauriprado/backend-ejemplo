using BackendEjemplo.ManyToManyExample.Domain.Models;
using BackendEjemplo.ManyToManyExample.Domain.Repositories;
using BackendEjemplo.Shared.Persistence.Context;
using BackendEjemplo.Shared.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BackendEjemplo.ManyToManyExample.Persistence.Repositories
{
    public class CourseRepository(AppDbContext context) : BaseRepository<Course>(context), ICourseRepository
    {
        public async Task<Course?> FindByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _context.Courses.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }
    }
}
