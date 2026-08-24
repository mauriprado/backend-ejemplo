using BackendEjemplo.OneToOneExample.Domain.Models;
using BackendEjemplo.OneToOneExample.Domain.Services.Communication;
using BackendEjemplo.Shared.Domain.Repositories;
using BackendEjemplo.Shared.Domain.Services.Communication;

namespace BackendEjemplo.OneToOneExample.Domain.Services
{
    public interface IEmployeeProfileService
    {
        Task<Page<EmployeeProfile>> ListPageAsync(EmployeeProfilePageRequest request, CancellationToken cancellationToken = default);
        Task<BaseResponse<EmployeeProfile>> FindByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<BaseResponse<EmployeeProfile>> AddAsync(EmployeeProfile profile, CancellationToken cancellationToken = default);
        Task<BaseResponse<EmployeeProfile>> UpdateAsync(long id, EmployeeProfile profile, CancellationToken cancellationToken = default);
        Task<BaseResponse<EmployeeProfile>> DeleteAsync(long id, CancellationToken cancellationToken = default);
    }
}
