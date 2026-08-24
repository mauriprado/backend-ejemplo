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
    public class EmployeeProfileService(
        IEmployeeProfileRepository employeeProfileRepository,
        IEmployeeRepository employeeRepository,
        IUnitOfWork unitOfWork) : IEmployeeProfileService
    {
        private static readonly Dictionary<string, Expression<Func<EmployeeProfile, object>>> SortableColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = p => p.Id,
            ["employeeId"] = p => p.EmployeeId,
            ["birthDate"] = p => p.BirthDate,
            ["address"] = p => p.Address,
            ["phoneNumber"] = p => p.PhoneNumber
        };

        public async Task<BaseResponse<EmployeeProfile>> AddAsync(EmployeeProfile profile, CancellationToken cancellationToken = default)
        {
            var existingEmployee = await employeeRepository.FindByIdAsync(profile.EmployeeId, cancellationToken);
            if (existingEmployee is null) return new BaseResponse<EmployeeProfile>($"Empleado con id {profile.EmployeeId} no existe");

            // Regla propia del uno a uno: un empleado no puede tener más de un
            // perfil (respaldado también por el índice único de la FK en la base
            // de datos que exige EF Core para esta relación).
            var existingProfile = await employeeProfileRepository.ListPageAsync(
                0, 1, p => p.EmployeeId == profile.EmployeeId, cancellationToken: cancellationToken);

            if (existingProfile.TotalRecords > 0)
                return new BaseResponse<EmployeeProfile>(
                    $"El empleado con id {profile.EmployeeId} ya tiene un perfil registrado", isConflict: true);

            await employeeProfileRepository.AddAsync(profile, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);

            // Employee ya queda asignado en profile por el "fixup" automático de EF Core:
            // existingEmployee está trackeado en el mismo DbContext.
            return new BaseResponse<EmployeeProfile>(profile);
        }

        public async Task<BaseResponse<EmployeeProfile>> DeleteAsync(long id, CancellationToken cancellationToken = default)
        {
            var existingProfile = await employeeProfileRepository.FindByIdAsync(id, cancellationToken);

            if (existingProfile is null) return new BaseResponse<EmployeeProfile>($"Perfil con id {id} no existe");

            employeeProfileRepository.Remove(existingProfile);
            await unitOfWork.CompleteAsync(cancellationToken);

            return new BaseResponse<EmployeeProfile>(existingProfile);
        }

        public async Task<BaseResponse<EmployeeProfile>> FindByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            var existingProfile = await employeeProfileRepository.FindByIdAsync(id, cancellationToken);

            if (existingProfile is null) return new BaseResponse<EmployeeProfile>($"Perfil con id {id} no existe");

            return new BaseResponse<EmployeeProfile>(existingProfile);
        }

        public async Task<Page<EmployeeProfile>> ListPageAsync(EmployeeProfilePageRequest request, CancellationToken cancellationToken = default)
        {
            Expression<Func<EmployeeProfile, bool>>? filter;

            filter = profile =>
            (!request.EmployeeId.HasValue || profile.EmployeeId == request.EmployeeId.Value) &&
            (string.IsNullOrWhiteSpace(request.Address) || profile.Address.Contains(request.Address)) &&
            (string.IsNullOrWhiteSpace(request.PhoneNumber) || profile.PhoneNumber.Contains(request.PhoneNumber));

            var page = await employeeProfileRepository.ListPageAsync(
                request.PageIndex,
                request.PageSize,
                filter,
                orderBy: q => q.ApplySort(request.SortBy, request.SortDescending, SortableColumns, defaultColumn: p => p.Id),
                cancellationToken: cancellationToken);

            return page;
        }

        public async Task<BaseResponse<EmployeeProfile>> UpdateAsync(long id, EmployeeProfile profile, CancellationToken cancellationToken = default)
        {
            var existingProfile = await employeeProfileRepository.FindByIdAsync(id, cancellationToken);

            if (existingProfile is null) return new BaseResponse<EmployeeProfile>($"Perfil con id {id} no existe");

            existingProfile.Biography = profile.Biography;
            existingProfile.Address = profile.Address;
            existingProfile.PhoneNumber = profile.PhoneNumber;
            existingProfile.BirthDate = profile.BirthDate;

            employeeProfileRepository.Update(existingProfile);
            await unitOfWork.CompleteAsync(cancellationToken);

            return new BaseResponse<EmployeeProfile>(existingProfile);
        }
    }
}
