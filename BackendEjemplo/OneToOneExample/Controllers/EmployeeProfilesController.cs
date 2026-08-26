using BackendEjemplo.OneToOneExample.Domain.Services;
using BackendEjemplo.OneToOneExample.Domain.Services.Communication;
using BackendEjemplo.OneToOneExample.Mapping;
using BackendEjemplo.OneToOneExample.Resources;
using BackendEjemplo.Shared.Domain.Services.Communication;
using BackendEjemplo.Shared.Extensions;
using BackendEjemplo.Shared.Mapping;
using Microsoft.AspNetCore.Mvc;

namespace BackendEjemplo.OneToOneExample.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class EmployeeProfilesController(IEmployeeProfileService employeeProfileService): ControllerBase
    {
        [HttpGet]
        public async Task<PageResponse<EmployeeProfileResource>> GetProfilesPaginatedAsync([FromQuery] EmployeeProfilePageRequest request, CancellationToken cancellationToken)
        {
            var result = await employeeProfileService.ListPageAsync(request, cancellationToken);
            return result.ToResponse(p => p.ToResource());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetByIdAsync(long id, CancellationToken cancellationToken)
        {
            var result = await employeeProfileService.FindByIdAsync(id, cancellationToken);

            if (!result.Success)
                return this.ToProblem(result, StatusCodes.Status404NotFound);

            return Ok(result.Content.ToResource());
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] SaveEmployeeProfileResource resource, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var profile = resource.ToEntity();
            var result = await employeeProfileService.AddAsync(profile, cancellationToken);

            if (!result.Success)
                return this.ToProblem(result, StatusCodes.Status400BadRequest);

            return Created($"/api/v1/employeeprofiles/{result.Content.Id}", result.Content.ToResource());
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutAsync(long id, [FromBody] SaveEmployeeProfileResource resource, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var profile = resource.ToEntity();
            var result = await employeeProfileService.UpdateAsync(id, profile, cancellationToken);

            if (!result.Success)
                return this.ToProblem(result, StatusCodes.Status404NotFound);

            return Ok(result.Content.ToResource());
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(long id, CancellationToken cancellationToken)
        {
            var result = await employeeProfileService.DeleteAsync(id, cancellationToken);

            if (!result.Success)
                return this.ToProblem(result, StatusCodes.Status404NotFound);

            return Ok(result.Content.ToResource());
        }
    }
}
