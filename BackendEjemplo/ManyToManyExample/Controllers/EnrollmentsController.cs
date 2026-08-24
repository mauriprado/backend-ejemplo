using BackendEjemplo.ManyToManyExample.Domain.Services;
using BackendEjemplo.ManyToManyExample.Domain.Services.Communication;
using BackendEjemplo.ManyToManyExample.Mapping;
using BackendEjemplo.ManyToManyExample.Resources;
using BackendEjemplo.Shared.Domain.Services.Communication;
using BackendEjemplo.Shared.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BackendEjemplo.ManyToManyExample.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class EnrollmentsController(IEnrollmentService enrollmentService): ControllerBase
    {
        [HttpGet]
        public async Task<PageResponse<EnrollmentResource>> GetEnrollmentsPaginatedAsync([FromQuery] EnrollmentPageRequest request, CancellationToken cancellationToken)
        {
            var result = await enrollmentService.ListPageAsync(request, cancellationToken);

            return new PageResponse<EnrollmentResource>
            {
                Data = result.Data.Select(p => p.ToResource()),
                PageIndex = result.PageIndex,
                PageSize = result.PageSize,
                TotalRecords = result.TotalRecords
            };
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] SaveEnrollmentResource resource, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var enrollment = resource.ToEntity();
            var result = await enrollmentService.AddAsync(enrollment, cancellationToken);

            if (!result.Success)
                return this.ToProblem(result, StatusCodes.Status400BadRequest);

            var enrollmentResource = result.Content.ToResource();

            return Created($"/api/v1/enrollments/{enrollmentResource.Id}", enrollmentResource);
        }

        [HttpPatch("state/{id}")]
        public async Task<IActionResult> PatchStateAsync(long id, [FromBody] ChangeEnrollmentStateRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var result = await enrollmentService.ChangeStateAsync(id, request.State, cancellationToken);

            if (!result.Success)
                return this.ToProblem(result, StatusCodes.Status404NotFound);

            var enrollmentResource = result.Content.ToResource();

            return Ok(enrollmentResource);
        }
    }
}
