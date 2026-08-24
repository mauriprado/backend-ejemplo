using BackendEjemplo.ManyToManyExample.Domain.Models;
using BackendEjemplo.ManyToManyExample.Domain.Repositories;
using BackendEjemplo.Shared.Persistence.Context;
using BackendEjemplo.Shared.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BackendEjemplo.ManyToManyExample.Persistence.Repositories
{
    public class StudentRepository(AppDbContext context) : BaseRepository<Student>(context), IStudentRepository
    {
        public async Task<Student?> FindByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _context.Students.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }
    }
}
