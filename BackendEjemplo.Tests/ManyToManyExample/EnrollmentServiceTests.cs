using BackendEjemplo.ManyToManyExample.Domain.Enums;
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
    public class EnrollmentServiceTests
    {
        private readonly Mock<IEnrollmentRepository> _enrollmentRepository = new();
        private readonly Mock<IStudentRepository> _studentRepository = new();
        private readonly Mock<ICourseRepository> _courseRepository = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly EnrollmentService _sut;

        public EnrollmentServiceTests()
        {
            _sut = new EnrollmentService(_enrollmentRepository.Object, _studentRepository.Object, _courseRepository.Object, _unitOfWork.Object);
        }

        private static Student SampleStudent(long id = 1) => new() { Id = id, FirstName = "Juan", LastName = "Perez" };
        private static Course SampleCourse(long id = 1) => new() { Id = id, Name = "Matematicas", Code = "MAT101" };

        private static Enrollment SampleEnrollment(long id = 1, long studentId = 1, long courseId = 1) => new()
        {
            Id = id,
            StudentId = studentId,
            CourseId = courseId,
            EnrollmentDate = DateTime.UtcNow,
            State = EnrollmentState.Active
        };

        private void SetupNoDuplicate() =>
            _enrollmentRepository.Setup(r => r.ListPageAsync(0, 1, It.IsAny<System.Linq.Expressions.Expression<Func<Enrollment, bool>>>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(RepositoryMockExtensions.EmptyPage<Enrollment>());

        [Fact]
        public async Task AddAsync_WhenStudentDoesNotExist_DoesNotPersist()
        {
            _studentRepository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Student?)null);

            var result = await _sut.AddAsync(SampleEnrollment(), TestContext.Current.CancellationToken);

            result.Success.Should().BeFalse();
            result.IsConflict.Should().BeFalse();
            _enrollmentRepository.Verify(r => r.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AddAsync_WhenCourseDoesNotExist_DoesNotPersist()
        {
            _studentRepository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(SampleStudent());
            _courseRepository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Course?)null);

            var result = await _sut.AddAsync(SampleEnrollment(), TestContext.Current.CancellationToken);

            result.Success.Should().BeFalse();
            result.IsConflict.Should().BeFalse();
            _enrollmentRepository.Verify(r => r.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AddAsync_WhenAlreadyEnrolled_ReturnsConflictAndDoesNotDuplicate()
        {
            _studentRepository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(SampleStudent());
            _courseRepository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(SampleCourse());
            _enrollmentRepository.Setup(r => r.ListPageAsync(0, 1, It.IsAny<System.Linq.Expressions.Expression<Func<Enrollment, bool>>>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Page<Enrollment> { Data = [SampleEnrollment()], PageIndex = 0, PageSize = 1, TotalRecords = 1 });

            var result = await _sut.AddAsync(SampleEnrollment(), TestContext.Current.CancellationToken);

            // Esta es la regla propia del muchos a muchos: el mismo alumno no puede
            // inscribirse dos veces al mismo curso (respaldada además por el índice
            // único en la base). El pre-check debe atajarlo antes del insert.
            result.Success.Should().BeFalse();
            result.IsConflict.Should().BeTrue();
            _enrollmentRepository.Verify(r => r.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AddAsync_WhenValid_PersistsAndReturnsSuccess()
        {
            _studentRepository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(SampleStudent());
            _courseRepository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(SampleCourse());
            SetupNoDuplicate();
            var enrollment = SampleEnrollment();

            var result = await _sut.AddAsync(enrollment, TestContext.Current.CancellationToken);

            result.Success.Should().BeTrue();
            _enrollmentRepository.Verify(r => r.AddAsync(enrollment, It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ChangeStateAsync_WhenNotFound_DoesNotPersist()
        {
            _enrollmentRepository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Enrollment?)null);

            var result = await _sut.ChangeStateAsync(1, EnrollmentState.Completed, TestContext.Current.CancellationToken);

            result.Success.Should().BeFalse();
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ChangeStateAsync_WhenFound_UpdatesStateAndPersists()
        {
            var enrollment = SampleEnrollment();
            _enrollmentRepository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(enrollment);

            var result = await _sut.ChangeStateAsync(1, EnrollmentState.Completed, TestContext.Current.CancellationToken);

            result.Success.Should().BeTrue();
            enrollment.State.Should().Be(EnrollmentState.Completed);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ListPageAsync_FiltersByStudentAndCourse()
        {
            var getFilter = _enrollmentRepository.CaptureListPageFilter<IEnrollmentRepository, Enrollment>(RepositoryMockExtensions.EmptyPage<Enrollment>());

            await _sut.ListPageAsync(new EnrollmentPageRequest { StudentId = 1, CourseId = 2 }, TestContext.Current.CancellationToken);

            var filter = getFilter()!.Compile();
            filter(SampleEnrollment(studentId: 1, courseId: 2)).Should().BeTrue();
            filter(SampleEnrollment(studentId: 1, courseId: 3)).Should().BeFalse();
            filter(SampleEnrollment(studentId: 9, courseId: 2)).Should().BeFalse();
        }

        [Fact]
        public async Task ListPageAsync_WithNoFilters_MatchesEverything()
        {
            var getFilter = _enrollmentRepository.CaptureListPageFilter<IEnrollmentRepository, Enrollment>(RepositoryMockExtensions.EmptyPage<Enrollment>());

            await _sut.ListPageAsync(new EnrollmentPageRequest(), TestContext.Current.CancellationToken);

            getFilter()!.Compile()(SampleEnrollment()).Should().BeTrue();
        }
    }
}
