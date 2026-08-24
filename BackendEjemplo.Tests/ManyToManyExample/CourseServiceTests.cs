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
    public class CourseServiceTests
    {
        private readonly Mock<ICourseRepository> _courseRepository = new();
        private readonly Mock<IEnrollmentRepository> _enrollmentRepository = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly CourseService _sut;

        public CourseServiceTests()
        {
            _sut = new CourseService(_courseRepository.Object, _enrollmentRepository.Object, _unitOfWork.Object);
        }

        private static Course SampleCourse(long id = 1) => new() { Id = id, Name = "Matematicas", Code = "MAT101", Credits = 4 };

        [Fact]
        public async Task AddAsync_PersistsAndReturnsSuccess()
        {
            var result = await _sut.AddAsync(SampleCourse(), TestContext.Current.CancellationToken);

            result.Success.Should().BeTrue();
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenCourseHasEnrollments_ReturnsConflictAndDoesNotDelete()
        {
            var course = SampleCourse();
            _courseRepository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(course);
            _enrollmentRepository.Setup(r => r.ListPageAsync(0, 1, It.IsAny<System.Linq.Expressions.Expression<Func<Enrollment, bool>>>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Page<Enrollment> { Data = [new Enrollment()], PageIndex = 0, PageSize = 1, TotalRecords = 1 });

            var result = await _sut.DeleteAsync(1, TestContext.Current.CancellationToken);

            result.Success.Should().BeFalse();
            result.IsConflict.Should().BeTrue();
            _courseRepository.Verify(r => r.Remove(It.IsAny<Course>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenCourseHasNoEnrollments_RemovesAndPersists()
        {
            var course = SampleCourse();
            _courseRepository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(course);
            _enrollmentRepository.Setup(r => r.ListPageAsync(0, 1, It.IsAny<System.Linq.Expressions.Expression<Func<Enrollment, bool>>>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(RepositoryMockExtensions.EmptyPage<Enrollment>());

            var result = await _sut.DeleteAsync(1, TestContext.Current.CancellationToken);

            result.Success.Should().BeTrue();
            _courseRepository.Verify(r => r.Remove(course), Times.Once);
        }

        [Fact]
        public async Task ListPageAsync_WithNoFilters_MatchesEverything()
        {
            var getFilter = _courseRepository.CaptureListPageFilter<ICourseRepository, Course>(RepositoryMockExtensions.EmptyPage<Course>());

            await _sut.ListPageAsync(new CoursePageRequest(), TestContext.Current.CancellationToken);

            getFilter()!.Compile()(SampleCourse()).Should().BeTrue();
        }
    }
}
