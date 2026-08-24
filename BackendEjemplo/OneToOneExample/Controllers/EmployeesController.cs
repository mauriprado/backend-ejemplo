using BackendEjemplo.OneToOneExample.Domain.Services;
using BackendEjemplo.OneToOneExample.Domain.Services.Communication;
using BackendEjemplo.OneToOneExample.Mapping;
using BackendEjemplo.OneToOneExample.Resources;
using BackendEjemplo.Shared.Domain.Services.Communication;
using BackendEjemplo.Shared.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BackendEjemplo.OneToOneExample.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class EmployeesController(IEmployeeService employeeService): ControllerBase
    {
        [HttpGet]
        public async Task<PageResponse<EmployeeResource>> GetEmployeesPaginatedAsync([FromQuery] EmployeePageRequest request, CancellationToken cancellationToken)
        {
            var result = await employeeService.ListPageAsync(request, cancellationToken);
            return new PageResponse<EmployeeResource>
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
            var result = await employeeService.FindByIdAsync(id, cancellationToken);

            if (!result.Success)
                return this.ToProblem(result, StatusCodes.Status404NotFound);

            return Ok(result.Content.ToResource());
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] SaveEmployeeResource resource, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var employee = resource.ToEntity();
            var result = await employeeService.AddAsync(employee, cancellationToken);

            if (!result.Success)
                return this.ToProblem(result, StatusCodes.Status400BadRequest);

            return Created($"/api/v1/employees/{result.Content.Id}", result.Content.ToResource());
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutAsync(long id, [FromBody] SaveEmployeeResource resource, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var employee = resource.ToEntity();
            var result = await employeeService.UpdateAsync(id, employee, cancellationToken);

            if (!result.Success)
                return this.ToProblem(result, StatusCodes.Status404NotFound);

            return Ok(result.Content.ToResource());
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(long id, CancellationToken cancellationToken)
        {
            var result = await employeeService.DeleteAsync(id, cancellationToken);

            if (!result.Success)
                return this.ToProblem(result, StatusCodes.Status404NotFound);

            return Ok(result.Content.ToResource());
        }
    }
}
