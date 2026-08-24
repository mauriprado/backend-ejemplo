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
    public class CoursesController(ICourseService courseService): ControllerBase
    {
        [HttpGet]
        public async Task<PageResponse<CourseResource>> GetCoursesPaginatedAsync([FromQuery] CoursePageRequest request, CancellationToken cancellationToken)
        {
            var result = await courseService.ListPageAsync(request, cancellationToken);
            return new PageResponse<CourseResource>
            {
                Data = result.Data.Select(p => p.ToResource()),
                PageIndex = result.PageIndex,
                PageSize = result.PageSize,
                TotalRecords = result.TotalRecords,
            };
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetByIdAsync(long id, CancellationToken cancellationToken)
        {
            var result = await courseService.FindByIdAsync(id, cancellationToken);

            if (!result.Success)
                return this.ToProblem(result, StatusCodes.Status404NotFound);

            return Ok(result.Content.ToResource());
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] SaveCourseResource resource, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var course = resource.ToEntity();
            var result = await courseService.AddAsync(course, cancellationToken);

            if (!result.Success)
                return this.ToProblem(result, StatusCodes.Status400BadRequest);

            return Created($"/api/v1/courses/{result.Content.Id}", result.Content.ToResource());
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutAsync(long id, [FromBody] SaveCourseResource resource, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var course = resource.ToEntity();
            var result = await courseService.UpdateAsync(id, course, cancellationToken);

            if (!result.Success)
                return this.ToProblem(result, StatusCodes.Status404NotFound);

            return Ok(result.Content.ToResource());
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(long id, CancellationToken cancellationToken)
        {
            var result = await courseService.DeleteAsync(id, cancellationToken);

            if (!result.Success)
                return this.ToProblem(result, StatusCodes.Status404NotFound);

            return Ok(result.Content.ToResource());
        }
    }
}
