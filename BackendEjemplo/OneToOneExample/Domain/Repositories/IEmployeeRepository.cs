using BackendEjemplo.OneToOneExample.Domain.Models;
using BackendEjemplo.Shared.Domain.Repositories;

namespace BackendEjemplo.OneToOneExample.Domain.Repositories
{
    public interface IEmployeeRepository: IBaseRepository<Employee>
    {
        Task<Employee?> FindByIdAsync(long id, CancellationToken cancellationToken = default);
    }
}
