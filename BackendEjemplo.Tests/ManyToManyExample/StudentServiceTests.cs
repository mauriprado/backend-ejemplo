using BackendEjemplo.ManyToManyExample.Domain.Models;
using BackendEjemplo.ManyToManyExample.Domain.Repositories;
using BackendEjemplo.ManyToManyExample.Domain.Services.Communication;
using BackendEjemplo.ManyToManyExample.Services;
using BackendEjemplo.Shared.Domain.Repositories;
using BackendEjemplo.Tests.TestHelpers;
using AwesomeAssertions;
using Moq;

namespace BackendEjemplo.Tests.ManyToManyExample
{
    public class StudentServiceTests
    {
        private readonly Mock<IStudentRepository> _studentRepository = new();
        private readonly Mock<IEnrollmentRepository> _enrollmentRepository = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly StudentService _sut;

        public StudentServiceTests()
        {
            _sut = new StudentService(_studentRepository.Object, _enrollmentRepository.Object, _unitOfWork.Object);
        }

        private static Student SampleStudent(long id = 1) => new() { Id = id, FirstName = "Juan", LastName = "Perez", Email = "juan@test.com" };

        [Fact]
        public async Task AddAsync_PersistsAndReturnsSuccess()
        {
            var student = SampleStudent();

            var result = await _sut.AddAsync(student, TestContext.Current.CancellationToken);

            result.Success.Should().BeTrue();
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenStudentHasEnrollments_ReturnsConflictAndDoesNotDelete()
        {
            var student = SampleStudent();
            _studentRepository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(student);
            _enrollmentRepository.Setup(r => r.ListPageAsync(0, 1, It.IsAny<System.Linq.Expressions.Expression<Func<Enrollment, bool>>>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Page<Enrollment> { Data = [new Enrollment()], PageIndex = 0, PageSize = 1, TotalRecords = 1 });

            var result = await _sut.DeleteAsync(1, TestContext.Current.CancellationToken);

            result.Success.Should().BeFalse();
            result.IsConflict.Should().BeTrue();
            _studentRepository.Verify(r => r.Remove(It.IsAny<Student>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenStudentHasNoEnrollments_RemovesAndPersists()
        {
            var student = SampleStudent();
            _studentRepository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(student);
            _enrollmentRepository.Setup(r => r.ListPageAsync(0, 1, It.IsAny<System.Linq.Expressions.Expression<Func<Enrollment, bool>>>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(RepositoryMockExtensions.EmptyPage<Enrollment>());

            var result = await _sut.DeleteAsync(1, TestContext.Current.CancellationToken);

            result.Success.Should().BeTrue();
            _studentRepository.Verify(r => r.Remove(student), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ListPageAsync_WithNoFilters_MatchesEverything()
        {
            var getFilter = _studentRepository.CaptureListPageFilter<IStudentRepository, Student>(RepositoryMockExtensions.EmptyPage<Student>());

            await _sut.ListPageAsync(new StudentPageRequest(), TestContext.Current.CancellationToken);

            getFilter()!.Compile()(SampleStudent()).Should().BeTrue();
        }
    }
}
