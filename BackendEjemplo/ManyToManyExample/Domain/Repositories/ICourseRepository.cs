using BackendEjemplo.ManyToManyExample.Domain.Models;
using BackendEjemplo.Shared.Domain.Repositories;

namespace BackendEjemplo.ManyToManyExample.Domain.Repositories
{
    public interface ICourseRepository: IBaseRepository<Course>
    {
        Task<Course?> FindByIdAsync(long id, CancellationToken cancellationToken = default);
    }
}
