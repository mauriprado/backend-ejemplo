using BackendEjemplo.Shared.Domain.Services.Communication;
using Microsoft.AspNetCore.Mvc;

namespace BackendEjemplo.Shared.Extensions
{
    public static class ControllerBaseExtensions
    {
        // Traduce un BaseResponse<T> fallido (Success == false) a un ProblemDetails
        // consistente con el resto de la API (mismo formato que ValidationProblem()
        // y que el 500 del GlobalExceptionHandler): 409 si es un conflicto de negocio
        // (IsConflict == true), o el status code que indique el caller — típicamente
        // 404 para "no existe" en GetById/Put/Delete, 400 para reglas de creación
        // (ej. FK a un padre inexistente).
        public static ObjectResult ToProblem<TEntity>(this ControllerBase controller, BaseResponse<TEntity> response, int failureStatusCode)
            where TEntity : class
        {
            var statusCode = response.IsConflict ? StatusCodes.Status409Conflict : failureStatusCode;
            return controller.Problem(detail: response.Message, statusCode: statusCode);
        }
    }
}
