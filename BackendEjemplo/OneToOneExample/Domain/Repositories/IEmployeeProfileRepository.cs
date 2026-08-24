using BackendEjemplo.OneToOneExample.Domain.Models;
using BackendEjemplo.Shared.Domain.Repositories;

namespace BackendEjemplo.OneToOneExample.Domain.Repositories
{
    public interface IEmployeeProfileRepository: IBaseRepository<EmployeeProfile>
    {
        Task<EmployeeProfile?> FindByIdAsync(long id, CancellationToken cancellationToken = default);
    }
}
