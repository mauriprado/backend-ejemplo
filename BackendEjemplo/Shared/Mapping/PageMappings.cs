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

        public static PageResponse<TResource> ToResponse<TEntity, TResource>(this Page<TEntity> page, Func<TEntity, TResource> mapping)
            where TEntity : class
            where TResource : class => new()
            {
                Data = page.Data.Select(mapping),
                PageIndex = page.PageIndex,
                PageSize = page.PageSize,
                TotalRecords = page.TotalRecords
            };
    }
}
