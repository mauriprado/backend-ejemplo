using BackendEjemplo.BotAudit.Domain.Services;
using BackendEjemplo.BotAudit.Services;
using BackendEjemplo.OneToManyExample.Domain.Services;
using BackendEjemplo.OneToManyExample.Domain.Services.Communication;
using BackendEjemplo.OneToManyExample.Mapping;
using BackendEjemplo.OneToManyExample.Resources;
using BackendEjemplo.Shared.Domain.Services.Communication;
using BackendEjemplo.Shared.Extensions;
using BackendEjemplo.Shared.Mapping;
using Microsoft.AspNetCore.Mvc;

namespace BackendEjemplo.OneToManyExample.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ClientsController(IClientService clientService): ControllerBase
    {
        [HttpGet]
        public async Task<PageResponse<ClientResource>> GetClientsPaginatedAsync([FromQuery] ClientPageRequest request, CancellationToken cancellationToken)
        {
            var result = await clientService.ListPageAsync(request, cancellationToken);
            return result.ToResponse(p => p.ToResource());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetByIdAsync(long id, CancellationToken cancellationToken)
        {
            var result = await clientService.FindByIdAsync(id, cancellationToken);

            if (!result.Success)
                return this.ToProblem(result, StatusCodes.Status404NotFound);

            return Ok(result.Content.ToResource());
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] SaveClientResource resource, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var client = resource.ToEntity();
            var result = await clientService.AddAsync(client, cancellationToken);

            if (!result.Success)
                return this.ToProblem(result, StatusCodes.Status400BadRequest);

            return Created($"/api/v1/clients/{result.Content.Id}", result.Content.ToResource());
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutAsync(long id, [FromBody] SaveClientResource resource, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var client = resource.ToEntity();
            var result = await clientService.UpdateAsync(id, client, cancellationToken);

            if (!result.Success)
                return this.ToProblem(result, StatusCodes.Status404NotFound);

            return Ok(result.Content.ToResource());
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(long id, CancellationToken cancellationToken)
        {
            var result = await clientService.DeleteAsync(id, cancellationToken);

            if (!result.Success)
                return this.ToProblem(result, StatusCodes.Status404NotFound);

            return Ok(result.Content.ToResource());
        }
    }
}
