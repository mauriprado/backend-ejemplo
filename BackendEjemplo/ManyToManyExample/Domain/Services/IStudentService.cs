using BackendEjemplo.ManyToManyExample.Domain.Models;
using BackendEjemplo.ManyToManyExample.Domain.Services.Communication;
using BackendEjemplo.Shared.Domain.Repositories;
using BackendEjemplo.Shared.Domain.Services.Communication;

namespace BackendEjemplo.ManyToManyExample.Domain.Services
{
    public interface IStudentService
    {
        Task<Page<Student>> ListPageAsync(StudentPageRequest request, CancellationToken cancellationToken = default);
        Task<BaseResponse<Student>> FindByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<BaseResponse<Student>> AddAsync(Student student, CancellationToken cancellationToken = default);
        Task<BaseResponse<Student>> UpdateAsync(long id, Student student, CancellationToken cancellationToken = default);
        Task<BaseResponse<Student>> DeleteAsync(long id, CancellationToken cancellationToken = default);
    }
}
