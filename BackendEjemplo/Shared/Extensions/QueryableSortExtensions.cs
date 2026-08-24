using System.Linq.Expressions;

namespace BackendEjemplo.Shared.Extensions
{
    // Aplica el ordenamiento pedido por el cliente (SortBy/SortDescending del
    // BasePageRequest) contra una whitelist de columnas ordenables definida por
    // cada Service. La whitelist es la razón de ser de este helper: evita que el
    // cliente pueda pedir orden por una propiedad de navegación pesada, un campo
    // no expuesto, o un nombre inventado que rompa la query. Si SortBy viene vacío
    // o no matchea ninguna clave de la whitelist, se cae a defaultColumn.
    //
    // Caer a un orden por defecto (nunca "sin orden") es intencional: sin ORDER BY,
    // Postgres no garantiza el mismo orden de filas entre una página y la siguiente
    // (puede repetir o saltear registros al paginar). Cada Service que no tenía un
    // orderBy explícito antes de esta mejora ahora pasa `e => e.Id` como default.
    public static class QueryableSortExtensions
    {
        public static IOrderedQueryable<TEntity> ApplySort<TEntity>(
            this IQueryable<TEntity> query,
            string? sortBy,
            bool sortDescending,
            IReadOnlyDictionary<string, Expression<Func<TEntity, object>>> sortableColumns,
            Expression<Func<TEntity, object>> defaultColumn,
            bool defaultDescending = false)
        {
            // El flag SortDescending del request solo aplica cuando el cliente pidió
            // explícitamente un SortBy reconocido. Si no, se respeta defaultDescending
            // (el orden por defecto que cada Service tenía antes de esta mejora, ej.
            // "más reciente primero" en Order/Enrollment) en vez de forzarlo siempre a
            // ascendente por el valor por defecto (false) de SortDescending.
            Expression<Func<TEntity, object>> column;
            bool descending;

            if (!string.IsNullOrWhiteSpace(sortBy) && sortableColumns.TryGetValue(sortBy, out var expression))
            {
                column = expression;
                descending = sortDescending;
            }
            else
            {
                column = defaultColumn;
                descending = defaultDescending;
            }

            return descending ? query.OrderByDescending(column) : query.OrderBy(column);
        }
    }
}
