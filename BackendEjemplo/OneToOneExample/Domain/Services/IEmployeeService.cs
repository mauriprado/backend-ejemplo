using BackendEjemplo.OneToOneExample.Domain.Models;
using BackendEjemplo.OneToOneExample.Domain.Services.Communication;
using BackendEjemplo.Shared.Domain.Repositories;
using BackendEjemplo.Shared.Domain.Services.Communication;

namespace BackendEjemplo.OneToOneExample.Domain.Services
{
    public interface IEmployeeService
    {
        Task<Page<Employee>> ListPageAsync(EmployeePageRequest request, CancellationToken cancellationToken = default);
        Task<BaseResponse<Employee>> FindByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<BaseResponse<Employee>> AddAsync(Employee employee, CancellationToken cancellationToken = default);
        Task<BaseResponse<Employee>> UpdateAsync(long id, Employee employee, CancellationToken cancellationToken = default);
        Task<BaseResponse<Employee>> DeleteAsync(long id, CancellationToken cancellationToken = default);
    }
}
