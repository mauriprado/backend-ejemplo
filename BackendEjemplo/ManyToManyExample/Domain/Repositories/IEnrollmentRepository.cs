using BackendEjemplo.ManyToManyExample.Domain.Models;
using BackendEjemplo.Shared.Domain.Repositories;

namespace BackendEjemplo.ManyToManyExample.Domain.Repositories
{
    public interface IEnrollmentRepository: IBaseRepository<Enrollment>
    {
        Task<Enrollment?> FindByIdAsync(long id, CancellationToken cancellationToken = default);
    }
}
