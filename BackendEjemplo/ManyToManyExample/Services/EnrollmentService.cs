using BackendEjemplo.ManyToManyExample.Domain.Enums;
using BackendEjemplo.ManyToManyExample.Domain.Models;
using BackendEjemplo.ManyToManyExample.Domain.Repositories;
using BackendEjemplo.ManyToManyExample.Domain.Services;
using BackendEjemplo.ManyToManyExample.Domain.Services.Communication;
using BackendEjemplo.Shared.Domain.Repositories;
using BackendEjemplo.Shared.Domain.Services.Communication;
using BackendEjemplo.Shared.Extensions;
using System.Linq.Expressions;

namespace BackendEjemplo.ManyToManyExample.Services
{
    public class EnrollmentService(
        IEnrollmentRepository enrollmentRepository,
        IStudentRepository studentRepository,
        ICourseRepository courseRepository,
        IUnitOfWork unitOfWork) : IEnrollmentService
    {
        private static readonly Dictionary<string, Expression<Func<Enrollment, object>>> SortableColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = e => e.Id,
            ["enrollmentDate"] = e => e.EnrollmentDate,
            ["state"] = e => e.State
        };

        public async Task<BaseResponse<Enrollment>> AddAsync(Enrollment enrollment, CancellationToken cancellationToken = default)
        {
            var existingStudent = await studentRepository.FindByIdAsync(enrollment.StudentId, cancellationToken);
            if (existingStudent is null) return new BaseResponse<Enrollment>($"Alumno con id {enrollment.StudentId} no existe");

            var existingCourse = await courseRepository.FindByIdAsync(enrollment.CourseId, cancellationToken);
            if (existingCourse is null) return new BaseResponse<Enrollment>($"Curso con id {enrollment.CourseId} no existe");

            // Regla propia del muchos a muchos: no permitir inscribir dos veces
            // al mismo alumno en el mismo curso (respaldado también por el índice
            // único en la base de datos).
            var duplicateEnrollment = await enrollmentRepository.ListPageAsync(
                0, 1, e => e.StudentId == enrollment.StudentId && e.CourseId == enrollment.CourseId, cancellationToken: cancellationToken);

            if (duplicateEnrollment.TotalRecords > 0)
                return new BaseResponse<Enrollment>(
                    $"El alumno con id {enrollment.StudentId} ya está inscrito en el curso con id {enrollment.CourseId}", isConflict: true);

            await enrollmentRepository.AddAsync(enrollment, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);

            // Student/Course ya quedan asignados en enrollment por el "fixup" automático
            // de EF Core: existingStudent/existingCourse están trackeados en el mismo
            // DbContext, así que al agregar enrollment con esos Id's, EF enlaza las
            // referencias de navegación sin necesidad de un Include adicional.
            return new BaseResponse<Enrollment>(enrollment);
        }

        public async Task<BaseResponse<Enrollment>> ChangeStateAsync(long id, EnrollmentState state, CancellationToken cancellationToken = default)
        {
            var existingEnrollment = await enrollmentRepository.FindByIdAsync(id, cancellationToken);

            if (existingEnrollment is null) return new BaseResponse<Enrollment>($"Inscripción con id {id} no existe");

            existingEnrollment.State = state;

            enrollmentRepository.Update(existingEnrollment);
            await unitOfWork.CompleteAsync(cancellationToken);

            return new BaseResponse<Enrollment>(existingEnrollment);
        }

        public async Task<Page<Enrollment>> ListPageAsync(EnrollmentPageRequest request, CancellationToken cancellationToken = default)
        {
            Expression<Func<Enrollment, bool>>? filter;

            filter = enrollment =>
            (!request.StudentId.HasValue || enrollment.StudentId == request.StudentId.Value) &&
            (!request.CourseId.HasValue || enrollment.CourseId == request.CourseId.Value) &&
            (!request.State.HasValue || enrollment.State == request.State.Value) &&
            (!request.StartEnrollmentDate.HasValue || enrollment.EnrollmentDate >= request.StartEnrollmentDate.Value.ToStartOfBusinessDayUtc()) &&
            (!request.EndEnrollmentDate.HasValue || enrollment.EnrollmentDate <= request.EndEnrollmentDate.Value.ToEndOfBusinessDayUtc());

            var page = await enrollmentRepository.ListPageAsync(
                request.PageIndex,
                request.PageSize,
                filter,
                orderBy: q => q.ApplySort(request.SortBy, request.SortDescending, SortableColumns, defaultColumn: e => e.EnrollmentDate, defaultDescending: true),
                cancellationToken: cancellationToken
                );

            return page;
        }
    }
}
