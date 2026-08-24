using BackendEjemplo.ManyToManyExample.Domain.Enums;
using BackendEjemplo.ManyToManyExample.Domain.Models;
using BackendEjemplo.ManyToManyExample.Domain.Services.Communication;
using BackendEjemplo.Shared.Domain.Repositories;
using BackendEjemplo.Shared.Domain.Services.Communication;

namespace BackendEjemplo.ManyToManyExample.Domain.Services
{
    public interface IEnrollmentService
    {
        Task<Page<Enrollment>> ListPageAsync(EnrollmentPageRequest request, CancellationToken cancellationToken = default);
        Task<BaseResponse<Enrollment>> AddAsync(Enrollment enrollment, CancellationToken cancellationToken = default);
        Task<BaseResponse<Enrollment>> ChangeStateAsync(long id, EnrollmentState state, CancellationToken cancellationToken = default);
    }
}
