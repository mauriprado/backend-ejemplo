using BackendEjemplo.Shared.Domain.Repositories;
using BackendEjemplo.Shared.Domain.Services.Communication;

namespace BackendEjemplo.Shared.Mapping
{
    public static class PageMappings
    {
        public static PageResponse<TEntity> ToResponse<TEntity>(this Page<TEntity> page)
            where TEntity : class => new()
            {
                Data = page.Data,
                PageIndex = page.PageIndex,
                PageSize = page.PageSize,
                TotalRecords = page.TotalRecords
            };
    }
}
