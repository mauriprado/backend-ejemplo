using BackendEjemplo.OneToOneExample.Domain.Models;
using BackendEjemplo.OneToOneExample.Domain.Repositories;
using BackendEjemplo.OneToOneExample.Domain.Services;
using BackendEjemplo.OneToOneExample.Domain.Services.Communication;
using BackendEjemplo.Shared.Domain.Repositories;
using BackendEjemplo.Shared.Domain.Services.Communication;
using BackendEjemplo.Shared.Extensions;
using System.Linq.Expressions;

namespace BackendEjemplo.OneToOneExample.Services
{
    public class EmployeeService(
        IEmployeeRepository employeeRepository,
        IEmployeeProfileRepository employeeProfileRepository,
        IUnitOfWork unitOfWork) : IEmployeeService
    {
        private static readonly Dictionary<string, Expression<Func<Employee, object>>> SortableColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = e => e.Id,
            ["firstName"] = e => e.FirstName,
            ["lastName"] = e => e.LastName,
            ["email"] = e => e.Email,
            ["position"] = e => e.Position,
            ["hireDate"] = e => e.HireDate
        };

        public async Task<BaseResponse<Employee>> AddAsync(Employee employee, CancellationToken cancellationToken = default)
        {
            await employeeRepository.AddAsync(employee, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            return new BaseResponse<Employee>(employee);
        }

        public async Task<BaseResponse<Employee>> DeleteAsync(long id, CancellationToken cancellationToken = default)
        {
            var existingEmployee = await employeeRepository.FindByIdAsync(id, cancellationToken);

            if (existingEmployee is null) return new BaseResponse<Employee>($"Empleado con id {id} no existe");

            var employeeProfile = await employeeProfileRepository.ListPageAsync(0, 1, p => p.EmployeeId == id, cancellationToken: cancellationToken);

            if (employeeProfile.TotalRecords > 0)
                return new BaseResponse<Employee>($"No se puede eliminar el empleado con id {id} porque tiene un perfil asociado", isConflict: true);

            employeeRepository.Remove(existingEmployee);
            await unitOfWork.CompleteAsync(cancellationToken);

            return new BaseResponse<Employee>(existingEmployee);
        }

        public async Task<BaseResponse<Employee>> FindByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            var existingEmployee = await employeeRepository.FindByIdAsync(id, cancellationToken);

            if (existingEmployee is null) return new BaseResponse<Employee>($"Empleado con id {id} no existe");

            return new BaseResponse<Employee>(existingEmployee);
        }

        public async Task<Page<Employee>> ListPageAsync(EmployeePageRequest request, CancellationToken cancellationToken = default)
        {
            Expression<Func<Employee, bool>>? filter;

            filter = employee =>
            (string.IsNullOrWhiteSpace(request.FullName) || (employee.FirstName.Contains(request.FullName) || employee.LastName.Contains(request.FullName))) &&
            (string.IsNullOrWhiteSpace(request.Email) || employee.Email.Contains(request.Email)) &&
            (string.IsNullOrWhiteSpace(request.Position) || employee.Position.Contains(request.Position)) &&
            (!request.StartHireDate.HasValue || employee.HireDate >= request.StartHireDate.Value) &&
            (!request.EndHireDate.HasValue || employee.HireDate <= request.EndHireDate.Value);

            var page = await employeeRepository.ListPageAsync(
                request.PageIndex,
                request.PageSize,
                filter,
                orderBy: q => q.ApplySort(request.SortBy, request.SortDescending, SortableColumns, defaultColumn: e => e.Id),
                cancellationToken: cancellationToken);

            return page;
        }

        public async Task<BaseResponse<Employee>> UpdateAsync(long id, Employee employee, CancellationToken cancellationToken = default)
        {
            var existingEmployee = await employeeRepository.FindByIdAsync(id, cancellationToken);

            if (existingEmployee is null) return new BaseResponse<Employee>($"Empleado con id {id} no existe");

            existingEmployee.FirstName = employee.FirstName;
            existingEmployee.LastName = employee.LastName;
            existingEmployee.Email = employee.Email;
            existingEmployee.Position = employee.Position;
            existingEmployee.HireDate = employee.HireDate;

            employeeRepository.Update(existingEmployee);
            await unitOfWork.CompleteAsync(cancellationToken);

            return new BaseResponse<Employee>(existingEmployee);
        }
    }
}
