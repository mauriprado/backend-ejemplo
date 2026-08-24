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
    public class CourseService(
        ICourseRepository courseRepository,
        IEnrollmentRepository enrollmentRepository,
        IUnitOfWork unitOfWork) : ICourseService
    {
        private static readonly Dictionary<string, Expression<Func<Course, object>>> SortableColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = c => c.Id,
            ["name"] = c => c.Name,
            ["code"] = c => c.Code,
            ["credits"] = c => c.Credits
        };

        public async Task<BaseResponse<Course>> AddAsync(Course course, CancellationToken cancellationToken = default)
        {
            await courseRepository.AddAsync(course, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            return new BaseResponse<Course>(course);
        }

        public async Task<BaseResponse<Course>> DeleteAsync(long id, CancellationToken cancellationToken = default)
        {
            var existingCourse = await courseRepository.FindByIdAsync(id, cancellationToken);

            if (existingCourse is null) return new BaseResponse<Course>($"Curso con id {id} no existe");

            var courseEnrollments = await enrollmentRepository.ListPageAsync(0, 1, e => e.CourseId == id, cancellationToken: cancellationToken);

            if (courseEnrollments.TotalRecords > 0)
                return new BaseResponse<Course>($"No se puede eliminar el curso con id {id} porque tiene inscripciones asociadas", isConflict: true);

            courseRepository.Remove(existingCourse);
            await unitOfWork.CompleteAsync(cancellationToken);

            return new BaseResponse<Course>(existingCourse);
        }

        public async Task<BaseResponse<Course>> FindByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            var existingCourse = await courseRepository.FindByIdAsync(id, cancellationToken);

            if (existingCourse is null) return new BaseResponse<Course>($"Curso con id {id} no existe");

            return new BaseResponse<Course>(existingCourse);
        }

        public async Task<Page<Course>> ListPageAsync(CoursePageRequest request, CancellationToken cancellationToken = default)
        {
            Expression<Func<Course, bool>>? filter;

            filter = course =>
            (string.IsNullOrWhiteSpace(request.Name) || course.Name.Contains(request.Name)) &&
            (string.IsNullOrWhiteSpace(request.Code) || course.Code.Contains(request.Code));

            var page = await courseRepository.ListPageAsync(
                request.PageIndex,
                request.PageSize,
                filter,
                orderBy: q => q.ApplySort(request.SortBy, request.SortDescending, SortableColumns, defaultColumn: c => c.Id),
                cancellationToken: cancellationToken);

            return page;
        }

        public async Task<BaseResponse<Course>> UpdateAsync(long id, Course course, CancellationToken cancellationToken = default)
        {
            var existingCourse = await courseRepository.FindByIdAsync(id, cancellationToken);

            if (existingCourse is null) return new BaseResponse<Course>($"Curso con id {id} no existe");

            existingCourse.Name = course.Name;
            existingCourse.Code = course.Code;
            existingCourse.Credits = course.Credits;

            courseRepository.Update(existingCourse);
            await unitOfWork.CompleteAsync(cancellationToken);

            return new BaseResponse<Course>(existingCourse);
        }
    }
}
