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
    public class EmployeeProfileServiceTests
    {
        private readonly Mock<IEmployeeProfileRepository> _employeeProfileRepository = new();
        private readonly Mock<IEmployeeRepository> _employeeRepository = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly EmployeeProfileService _sut;

        public EmployeeProfileServiceTests()
        {
            _sut = new EmployeeProfileService(_employeeProfileRepository.Object, _employeeRepository.Object, _unitOfWork.Object);
        }

        private static Employee SampleEmployee(long id = 1) => new() { Id = id, FirstName = "Carlos", LastName = "Ramos" };

        private static EmployeeProfile SampleProfile(long id = 1, long employeeId = 1) => new()
        {
            Id = id,
            EmployeeId = employeeId,
            Biography = "bio",
            Address = "direccion",
            PhoneNumber = "999999999",
            BirthDate = DateTime.UtcNow
        };

        private void SetupNoExistingProfile() =>
            _employeeProfileRepository.Setup(r => r.ListPageAsync(0, 1, It.IsAny<System.Linq.Expressions.Expression<Func<EmployeeProfile, bool>>>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(RepositoryMockExtensions.EmptyPage<EmployeeProfile>());

        [Fact]
        public async Task AddAsync_WhenEmployeeDoesNotExist_DoesNotPersist()
        {
            _employeeRepository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Employee?)null);

            var result = await _sut.AddAsync(SampleProfile(), TestContext.Current.CancellationToken);

            result.Success.Should().BeFalse();
            result.IsConflict.Should().BeFalse();
            _employeeProfileRepository.Verify(r => r.AddAsync(It.IsAny<EmployeeProfile>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AddAsync_WhenEmployeeAlreadyHasProfile_ReturnsConflictAndDoesNotDuplicate()
        {
            _employeeRepository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(SampleEmployee());
            _employeeProfileRepository.Setup(r => r.ListPageAsync(0, 1, It.IsAny<System.Linq.Expressions.Expression<Func<EmployeeProfile, bool>>>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Page<EmployeeProfile> { Data = [SampleProfile()], PageIndex = 0, PageSize = 1, TotalRecords = 1 });

            var result = await _sut.AddAsync(SampleProfile(), TestContext.Current.CancellationToken);

            // Regla propia del uno a uno: un empleado no puede tener más de un
            // perfil (respaldada por el índice único de la FK en la base).
            result.Success.Should().BeFalse();
            result.IsConflict.Should().BeTrue();
            _employeeProfileRepository.Verify(r => r.AddAsync(It.IsAny<EmployeeProfile>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AddAsync_WhenValid_PersistsAndReturnsSuccess()
        {
            _employeeRepository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(SampleEmployee());
            SetupNoExistingProfile();
            var profile = SampleProfile();

            var result = await _sut.AddAsync(profile, TestContext.Current.CancellationToken);

            result.Success.Should().BeTrue();
            _employeeProfileRepository.Verify(r => r.AddAsync(profile, It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenNotFound_DoesNotPersist()
        {
            _employeeProfileRepository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((EmployeeProfile?)null);

            var result = await _sut.DeleteAsync(1, TestContext.Current.CancellationToken);

            result.Success.Should().BeFalse();
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenFound_RemovesWithoutAnyConflictCheck()
        {
            // A diferencia de Employee/Student/Course/Client, borrar un EmployeeProfile
            // no tiene pre-check de conflicto: nada depende de él en el modelo.
            var profile = SampleProfile();
            _employeeProfileRepository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(profile);

            var result = await _sut.DeleteAsync(1, TestContext.Current.CancellationToken);

            result.Success.Should().BeTrue();
            _employeeProfileRepository.Verify(r => r.Remove(profile), Times.Once);
        }

        [Fact]
        public async Task ListPageAsync_FiltersByEmployeeId()
        {
            var getFilter = _employeeProfileRepository.CaptureListPageFilter<IEmployeeProfileRepository, EmployeeProfile>(RepositoryMockExtensions.EmptyPage<EmployeeProfile>());

            await _sut.ListPageAsync(new EmployeeProfilePageRequest { EmployeeId = 1 }, TestContext.Current.CancellationToken);

            var filter = getFilter()!.Compile();
            filter(SampleProfile(employeeId: 1)).Should().BeTrue();
            filter(SampleProfile(employeeId: 2)).Should().BeFalse();
        }
    }
}
