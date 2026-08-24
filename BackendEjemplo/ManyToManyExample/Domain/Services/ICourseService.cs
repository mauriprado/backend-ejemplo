using BackendEjemplo.ManyToManyExample.Domain.Models;
using BackendEjemplo.ManyToManyExample.Domain.Services.Communication;
using BackendEjemplo.Shared.Domain.Repositories;
using BackendEjemplo.Shared.Domain.Services.Communication;

namespace BackendEjemplo.ManyToManyExample.Domain.Services
{
    public interface ICourseService
    {
        Task<Page<Course>> ListPageAsync(CoursePageRequest request, CancellationToken cancellationToken = default);
        Task<BaseResponse<Course>> FindByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<BaseResponse<Course>> AddAsync(Course course, CancellationToken cancellationToken = default);
        Task<BaseResponse<Course>> UpdateAsync(long id, Course course, CancellationToken cancellationToken = default);
        Task<BaseResponse<Course>> DeleteAsync(long id, CancellationToken cancellationToken = default);
    }
}
