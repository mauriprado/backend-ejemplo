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
    public class OrdersController(IOrderService orderService): ControllerBase
    {
        [HttpGet]
        public async Task<PageResponse<OrderResource>> GetOrdersPaginatedAsync([FromQuery] OrderPageRequest request, CancellationToken cancellationToken)
        {
            var result = await orderService.ListPageAsync(request, cancellationToken);
            return result.ToResponse(p => p.ToResource());

        }

        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] SaveOrderResource resource, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var order = resource.ToEntity();
            var result = await orderService.AddAsync(order, cancellationToken);

            if (!result.Success)
                return this.ToProblem(result, StatusCodes.Status400BadRequest);

            var orderResource = result.Content.ToResource();

            return Created($"/api/v1/orders/{orderResource.Id}", orderResource);
        }

        [HttpPatch("state/{id}")]
        public async Task<IActionResult> PatchStateAsync(long id, [FromBody] ChangeOrderStateRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var result = await orderService.ChangeStateAsync(id, request.State, cancellationToken);

            if (!result.Success)
                return this.ToProblem(result, StatusCodes.Status404NotFound);

            var orderResource = result.Content.ToResource();

            return Ok(orderResource);
        }
    }
}
