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
    public class StudentService(
        IStudentRepository studentRepository,
        IEnrollmentRepository enrollmentRepository,
        IUnitOfWork unitOfWork) : IStudentService
    {
        private static readonly Dictionary<string, Expression<Func<Student, object>>> SortableColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = s => s.Id,
            ["firstName"] = s => s.FirstName,
            ["lastName"] = s => s.LastName,
            ["email"] = s => s.Email
        };

        public async Task<BaseResponse<Student>> AddAsync(Student student, CancellationToken cancellationToken = default)
        {
            await studentRepository.AddAsync(student, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            return new BaseResponse<Student>(student);
        }

        public async Task<BaseResponse<Student>> DeleteAsync(long id, CancellationToken cancellationToken = default)
        {
            var existingStudent = await studentRepository.FindByIdAsync(id, cancellationToken);

            if (existingStudent is null) return new BaseResponse<Student>($"Alumno con id {id} no existe");

            var studentEnrollments = await enrollmentRepository.ListPageAsync(0, 1, e => e.StudentId == id, cancellationToken: cancellationToken);

            if (studentEnrollments.TotalRecords > 0)
                return new BaseResponse<Student>($"No se puede eliminar el alumno con id {id} porque tiene inscripciones asociadas", isConflict: true);

            studentRepository.Remove(existingStudent);
            await unitOfWork.CompleteAsync(cancellationToken);

            return new BaseResponse<Student>(existingStudent);
        }

        public async Task<BaseResponse<Student>> FindByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            var existingStudent = await studentRepository.FindByIdAsync(id, cancellationToken);

            if (existingStudent is null) return new BaseResponse<Student>($"Alumno con id {id} no existe");

            return new BaseResponse<Student>(existingStudent);
        }

        public async Task<Page<Student>> ListPageAsync(StudentPageRequest request, CancellationToken cancellationToken = default)
        {
            Expression<Func<Student, bool>>? filter;

            filter = student =>
            (string.IsNullOrWhiteSpace(request.FullName) || (student.FirstName.Contains(request.FullName) || student.LastName.Contains(request.FullName))) &&
            (string.IsNullOrWhiteSpace(request.Email) || student.Email.Contains(request.Email));

            var page = await studentRepository.ListPageAsync(
                request.PageIndex,
                request.PageSize,
                filter,
                orderBy: q => q.ApplySort(request.SortBy, request.SortDescending, SortableColumns, defaultColumn: s => s.Id),
                cancellationToken: cancellationToken);

            return page;
        }

        public async Task<BaseResponse<Student>> UpdateAsync(long id, Student student, CancellationToken cancellationToken = default)
        {
            var existingStudent = await studentRepository.FindByIdAsync(id, cancellationToken);

            if (existingStudent is null) return new BaseResponse<Student>($"Alumno con id {id} no existe");

            existingStudent.FirstName = student.FirstName;
            existingStudent.LastName = student.LastName;
            existingStudent.Email = student.Email;

            studentRepository.Update(existingStudent);
            await unitOfWork.CompleteAsync(cancellationToken);

            return new BaseResponse<Student>(existingStudent);
        }
    }
}
