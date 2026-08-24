using BackendEjemplo.Shared.Domain.Repositories;
using BackendEjemplo.Shared.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BackendEjemplo.Shared.Persistence.Repositories
{
    public abstract class BaseRepository<TEntity>(AppDbContext context)
        : IBaseRepository<TEntity> where TEntity : class
    {
        protected readonly AppDbContext _context = context;
        protected const int MaxPageSize = 100;

        public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            await _context.Set<TEntity>().AddAsync(entity, cancellationToken);
        }

        protected IQueryable<TEntity> GetQuery(
            Expression<Func<TEntity, bool>>? filter,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy
            )
        {
            // 1. Preparar la consulta base con AsNoTracking para optimizar lecturas
            var query = _context.Set<TEntity>().AsNoTracking();

            // 2. Aplicar filtro si existe (afecta tanto al conteo como a los datos)
            if (filter != null)
            {
                query = query.Where(filter);
            }

            // 3. Aplicar ordenamiento si existe
            if (orderBy != null)
            {
                query = orderBy(query);
            }

            // 4. Devolver la consulta para su posterior uso
            return query;
        }

        public virtual async Task<IEnumerable<TEntity>> ListAsync(
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            CancellationToken cancellationToken = default)
        {
            // 1. Obtener la consulta
            var query = GetQuery(filter, orderBy);

            // 2. Ejecutar la consulta en la base de datos
            return await query.ToListAsync(cancellationToken);
        }

        public virtual async Task<Page<TEntity>> ListPageAsync(
            int pageIndex,
            int pageSize,
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, // <--- Orden opcional
            CancellationToken cancellationToken = default)
        {
            // 1. Validaciones de paginación segura
            if (pageIndex < 0) pageIndex = 0;

            if (pageSize < 1) pageSize = 10;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;


            //2. Obtener la consulta
            var query = GetQuery(filter, orderBy);



            // 3. Contar el total de registros que cumplen el filtro.
            // No se puede paralelizar con Task.WhenAll: ambas consultas comparten la misma
            // instancia de DbContext, y EF Core no admite operaciones async concurrentes
            // sobre un mismo contexto (lanza "A second operation was started on this context...").
            var totalRecords = await query.CountAsync(cancellationToken);

            // 3.1 Clampear pageIndex a la última página válida. Sin esto, un pageIndex
            // absurdamente grande (ej. ?pageIndex=30000000&pageSize=100) hacía overflow
            // en "pageIndex * pageSize" (ambos int), el resultado negativo se pasaba a
            // Skip() y Postgres lo rechazaba con "OFFSET no debe ser negativo" (bug
            // documentado en ARCHITECTURE.md sección 7, ahora corregido). Además de
            // evitar el overflow, clampear es más correcto: pedir una página que no
            // existe ahora devuelve la última página válida en vez de un error.
            var totalPages = totalRecords == 0 ? 0 : (int)Math.Ceiling((double)totalRecords / pageSize);
            if (pageIndex > 0 && pageIndex >= totalPages) pageIndex = Math.Max(0, totalPages - 1);

            // 4. Aplicar paginación y obtener los datos
            var data = await query
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new Page<TEntity>
            {
                Data = data,
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalRecords = totalRecords
            };
        }


        public virtual void Remove(TEntity entity)
        {
            _context.Set<TEntity>().Remove(entity);
        }

        public virtual void Update(TEntity entity)
        {
            _context.Set<TEntity>().Update(entity);
        }
    }
}
