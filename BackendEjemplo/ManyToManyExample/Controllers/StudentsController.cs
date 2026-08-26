using BackendEjemplo.ManyToManyExample.Domain.Services;
using BackendEjemplo.ManyToManyExample.Domain.Services.Communication;
using BackendEjemplo.ManyToManyExample.Mapping;
using BackendEjemplo.ManyToManyExample.Resources;
using BackendEjemplo.Shared.Domain.Services.Communication;
using BackendEjemplo.Shared.Extensions;
using BackendEjemplo.Shared.Mapping;
using Microsoft.AspNetCore.Mvc;

namespace BackendEjemplo.ManyToManyExample.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class StudentsController(IStudentService studentService): ControllerBase
    {
        [HttpGet]
        public async Task<PageResponse<StudentResource>> GetStudentsPaginatedAsync([FromQuery] StudentPageRequest request, CancellationToken cancellationToken)
        {
            var result = await studentService.ListPageAsync(request, cancellationToken);
            return result.ToResponse(p => p.ToResource());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetByIdAsync(long id, CancellationToken cancellationToken)
        {
            var result = await studentService.FindByIdAsync(id, cancellationToken);

            if (!result.Success)
                return this.ToProblem(result, StatusCodes.Status404NotFound);

            return Ok(result.Content.ToResource());
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] SaveStudentResource resource, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var student = resource.ToEntity();
            var result = await studentService.AddAsync(student, cancellationToken);

            if (!result.Success)
                return this.ToProblem(result, StatusCodes.Status400BadRequest);

            return Created($"/api/v1/students/{result.Content.Id}", result.Content.ToResource());
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutAsync(long id, [FromBody] SaveStudentResource resource, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var student = resource.ToEntity();
            var result = await studentService.UpdateAsync(id, student, cancellationToken);

            if (!result.Success)
                return this.ToProblem(result, StatusCodes.Status404NotFound);

            return Ok(result.Content.ToResource());
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(long id, CancellationToken cancellationToken)
        {
            var result = await studentService.DeleteAsync(id, cancellationToken);

            if (!result.Success)
                return this.ToProblem(result, StatusCodes.Status404NotFound);

            return Ok(result.Content.ToResource());
        }
    }
}
