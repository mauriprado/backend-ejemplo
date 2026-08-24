using BackendEjemplo.BotAudit.Domain.Models;
using BackendEjemplo.BotAudit.Domain.Services;
using BackendEjemplo.BotAudit.Domain.Services.Communication;
using BackendEjemplo.BotAudit.Mapping;
using BackendEjemplo.BotAudit.Resources;
using BackendEjemplo.Shared.Domain.Services.Communication;
using BackendEjemplo.Shared.Extensions;
using BackendEjemplo.Shared.Mapping;
using Microsoft.AspNetCore.Mvc;

namespace BackendEjemplo.BotAudit.Controllers
{
    [ApiController]
    [Route("api/v1/bot_logs")]
    public class BotLogsController(IBotLogService botLogService) : ControllerBase
    {
        [HttpGet]
        public async Task<PageResponse<BotLog>> GetBotLogsPaginatedAsync([FromQuery] BotLogPageRequest request, CancellationToken cancellationToken)
        {
            var result = await botLogService.ListPageAsync(request, cancellationToken);
            return result.ToResponse();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetByIdAsync(long id, CancellationToken cancellationToken)
        {
            var result = await botLogService.FindByIdAsync(id, cancellationToken);

            if (!result.Success)
                return this.ToProblem(result, StatusCodes.Status404NotFound);

            return Ok(result.Content);
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] SaveBotLogResource resource, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var log = resource.ToEntity();
            var result = await botLogService.AddAsync(log, cancellationToken);

            if (!result.Success)
                return this.ToProblem(result, StatusCodes.Status400BadRequest);

            return Created($"/api/v1/bot_logs/{result.Content.Id}", result.Content);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutAsync(long id, [FromBody] SaveBotLogResource resource, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var log = resource.ToEntity();
            var result = await botLogService.UpdateAsync(id, log, cancellationToken);

            if (!result.Success)
                return this.ToProblem(result, StatusCodes.Status404NotFound);

            return Ok(result.Content);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(long id, CancellationToken cancellationToken)
        {
            var result = await botLogService.DeleteAsync(id, cancellationToken);

            if (!result.Success)
                return this.ToProblem(result, StatusCodes.Status404NotFound);

            return Ok(result.Content);
        }
    }
}
