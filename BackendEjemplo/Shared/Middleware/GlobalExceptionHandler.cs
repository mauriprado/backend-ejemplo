using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendEjemplo.Shared.Middleware
{
    public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IProblemDetailsService problemDetailsService) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            // El cliente cancelo el request (cerro la pestania, timeout, etc.) - no es un error real,
            // asi que no lo logueamos como tal y dejamos que ASP.NET Core simplemente corte la conexion.
            if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
                return true;

            // Concurrencia optimista (xmin, ver AppDbContext): otra request modificó o
            // borró la misma fila entre que esta la leyó y la guardó. Es una condición
            // esperable bajo uso concurrente, no un bug — se loguea como warning (no
            // error) y se traduce a 409 en vez de dejar que caiga en el 500 genérico.
            if (exception is DbUpdateConcurrencyException)
            {
                logger.LogWarning(exception, "Conflicto de concurrencia en {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

                httpContext.Response.StatusCode = StatusCodes.Status409Conflict;

                return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
                {
                    HttpContext = httpContext,
                    Exception = exception,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status409Conflict,
                        Title = "Conflict",
                        Detail = "El recurso fue modificado o eliminado por otro proceso mientras se procesaba esta solicitud. Volvé a cargarlo e intentá de nuevo.",
                        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10"
                    }
                });
            }

            logger.LogError(exception, "Error no controlado en {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

            // TryWriteAsync pasa por el mismo pipeline de ProblemDetails que usan
            // Problem()/ValidationProblem() en los controllers (registrado via
            // AddProblemDetails() en Program.cs) — mismo formato, mismo traceId,
            // en vez de armar el JSON a mano y arriesgarse a que se desincronice.
            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Ocurrió un error inesperado.",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
                }
            });
        }
    }
}
