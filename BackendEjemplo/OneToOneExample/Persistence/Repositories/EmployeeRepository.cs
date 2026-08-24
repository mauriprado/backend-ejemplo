using BackendEjemplo.OneToOneExample.Domain.Models;
using BackendEjemplo.OneToOneExample.Domain.Repositories;
using BackendEjemplo.Shared.Persistence.Context;
using BackendEjemplo.Shared.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BackendEjemplo.OneToOneExample.Persistence.Repositories
{
    public class EmployeeRepository(AppDbContext context) : BaseRepository<Employee>(context), IEmployeeRepository
    {
        public async Task<Employee?> FindByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _context.Employees.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }
    }
}
