using BackendEjemplo.OneToOneExample.Domain.Models;
using BackendEjemplo.OneToOneExample.Domain.Repositories;
using BackendEjemplo.OneToOneExample.Domain.Services.Communication;
using BackendEjemplo.OneToOneExample.Services;
using BackendEjemplo.Shared.Domain.Repositories;
using BackendEjemplo.Tests.TestHelpers;
using AwesomeAssertions;
using Moq;

namespace BackendEjemplo.Tests.OneToOneExample
{
    public class EmployeeServiceTests
    {
        private readonly Mock<IEmployeeRepository> _employeeRepository = new();
        private readonly Mock<IEmployeeProfileRepository> _employeeProfileRepository = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly EmployeeService _sut;

        public EmployeeServiceTests()
        {
            _sut = new EmployeeService(_employeeRepository.Object, _employeeProfileRepository.Object, _unitOfWork.Object);
        }

        private static Employee SampleEmployee(long id = 1) => new()
        {
            Id = id,
            FirstName = "Carlos",
            LastName = "Ramos",
            Email = "carlos@test.com",
            Position = "Backend Developer",
            HireDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        [Fact]
        public async Task AddAsync_PersistsAndReturnsSuccess()
        {
            var result = await _sut.AddAsync(SampleEmployee(), TestContext.Current.CancellationToken);

            result.Success.Should().BeTrue();
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenEmployeeHasProfile_ReturnsConflictAndDoesNotDelete()
        {
            var employee = SampleEmployee();
            _employeeRepository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(employee);
            _employeeProfileRepository.Setup(r => r.ListPageAsync(0, 1, It.IsAny<System.Linq.Expressions.Expression<Func<EmployeeProfile, bool>>>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Page<EmployeeProfile> { Data = [new EmployeeProfile()], PageIndex = 0, PageSize = 1, TotalRecords = 1 });

            var result = await _sut.DeleteAsync(1, TestContext.Current.CancellationToken);

            result.Success.Should().BeFalse();
            result.IsConflict.Should().BeTrue();
            _employeeRepository.Verify(r => r.Remove(It.IsAny<Employee>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenEmployeeHasNoProfile_RemovesAndPersists()
        {
            var employee = SampleEmployee();
            _employeeRepository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(employee);
            _employeeProfileRepository.Setup(r => r.ListPageAsync(0, 1, It.IsAny<System.Linq.Expressions.Expression<Func<EmployeeProfile, bool>>>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(RepositoryMockExtensions.EmptyPage<EmployeeProfile>());

            var result = await _sut.DeleteAsync(1, TestContext.Current.CancellationToken);

            result.Success.Should().BeTrue();
            _employeeRepository.Verify(r => r.Remove(employee), Times.Once);
        }

        [Fact]
        public async Task ListPageAsync_WithNoFilters_MatchesEverything()
        {
            var getFilter = _employeeRepository.CaptureListPageFilter<IEmployeeRepository, Employee>(RepositoryMockExtensions.EmptyPage<Employee>());

            await _sut.ListPageAsync(new EmployeePageRequest(), TestContext.Current.CancellationToken);

            getFilter()!.Compile()(SampleEmployee()).Should().BeTrue();
        }
    }
}
