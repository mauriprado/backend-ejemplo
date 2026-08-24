using BackendEjemplo.BotAudit.Domain.Models;
using BackendEjemplo.BotAudit.Domain.Repositories;
using BackendEjemplo.BotAudit.Domain.Services;
using BackendEjemplo.BotAudit.Domain.Services.Communication;
using BackendEjemplo.Shared.Domain.Repositories;
using BackendEjemplo.Shared.Domain.Services.Communication;
using BackendEjemplo.Shared.Extensions;
using BackendEjemplo.Shared.Mapping;
using System.Linq.Expressions;

namespace BackendEjemplo.BotAudit.Services
{
    public class BotLogService(
        IBotLogRepository botLogRepository,
        IUnitOfWork unitOfWork) : IBotLogService
    {
        private static readonly Dictionary<string, Expression<Func<BotLog, object>>> SortableColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = l => l.Id,
            ["fecha"] = l => l.Fecha,
            ["bot"] = l => l.Bot,
            ["server"] = l => l.Server,
            ["falla"] = l => l.Falla
        };

        public async Task<BaseResponse<BotLog>> AddAsync(BotLog botLog, CancellationToken cancellationToken = default)
        {
            await botLogRepository.AddAsync(botLog, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            return new BaseResponse<BotLog>(botLog);
        }

        public async Task<BaseResponse<BotLog>> DeleteAsync(long id, CancellationToken cancellationToken = default)
        {
            var existingLog = await botLogRepository.FindByIdAsync(id, cancellationToken);

            if (existingLog is null) return new BaseResponse<BotLog>($"Log con id {id} no existe");

            botLogRepository.Remove(existingLog);
            await unitOfWork.CompleteAsync(cancellationToken);

            return new BaseResponse<BotLog>(existingLog);
        }

        public async Task<BaseResponse<BotLog>> FindByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            var existingLog = await botLogRepository.FindByIdAsync(id, cancellationToken);

            if (existingLog is null) return new BaseResponse<BotLog>($"Log con id {id} no existe");

            return new BaseResponse<BotLog>(existingLog);
        }

        public async Task<Page<BotLog>> ListPageAsync(BotLogPageRequest request, CancellationToken cancellationToken = default)
        {
            Expression<Func<BotLog, bool>>? filter;

            filter = log =>
            (string.IsNullOrWhiteSpace(request.Bot) || log.Bot.Contains(request.Bot)) &&
            (string.IsNullOrWhiteSpace(request.Server) || log.Server.Contains(request.Server)) &&
            (!request.StartDate.HasValue || log.Fecha >= request.StartDate.Value.ToStartOfBusinessDayUtc()) &&
            (!request.EndDate.HasValue || log.Fecha <= request.EndDate.Value.ToEndOfBusinessDayUtc()) &&
            (string.IsNullOrWhiteSpace(request.Mensaje) || log.Mensaje.Contains(request.Mensaje)) &&
            (!request.Falla.HasValue || log.Falla == request.Falla);

            var page = await botLogRepository.ListPageAsync(
                request.PageIndex,
                request.PageSize,
                filter,
                orderBy: q => q.ApplySort(request.SortBy, request.SortDescending, SortableColumns, defaultColumn: l => l.Id),
                cancellationToken: cancellationToken);

            return page;
        }

        public async Task<BaseResponse<BotLog>> UpdateAsync(long id, BotLog botLog, CancellationToken cancellationToken = default)
        {
            var existingLog = await botLogRepository.FindByIdAsync(id, cancellationToken);

            if (existingLog is null) return new BaseResponse<BotLog>($"Log con id {id} no existe");

            existingLog.Bot = botLog.Bot;
            existingLog.Server = botLog.Server;
            existingLog.Subflujo = botLog.Subflujo;
            existingLog.UsuarioBot = botLog.UsuarioBot;
            existingLog.Plataforma = botLog.Plataforma;
            existingLog.UsuarioPlataforma = botLog.UsuarioPlataforma;
            existingLog.TipoDocumento = botLog.TipoDocumento;
            existingLog.NroDocumento = botLog.NroDocumento;
            existingLog.Mensaje = botLog.Mensaje;
            existingLog.Falla = botLog.Falla;

            botLogRepository.Update(existingLog);
            await unitOfWork.CompleteAsync(cancellationToken);

            return new BaseResponse<BotLog>(existingLog);
        }
    }
}
