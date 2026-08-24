using BackendEjemplo.ManyToManyExample.Domain.Models;
using BackendEjemplo.Shared.Domain.Repositories;

namespace BackendEjemplo.ManyToManyExample.Domain.Repositories
{
    public interface IStudentRepository: IBaseRepository<Student>
    {
        Task<Student?> FindByIdAsync(long id, CancellationToken cancellationToken = default);
    }
}
