using BackendEjemplo.Shared.Domain.Repositories;
using Moq;
using System.Linq.Expressions;

namespace BackendEjemplo.Tests.TestHelpers
{
    public static class RepositoryMockExtensions
    {
        // Intercepta el filtro (Expression<Func<TEntity, bool>>) que el Service arma y le
        // pasa a ListPageAsync, sin necesitar una base de datos real. El test compila esa
        // expresión y la corre contra POCOs en memoria para verificar el comportamiento
        // real del filtro — exactamente el tipo de test que habría atrapado el bug del
        // "!string.IsNullOrWhiteSpace(x) || x.Contains(x)" (filtro invertido) que se
        // coló en este proyecto y hacía que el listado devolviera 0 filas sin filtros.
        public static Func<Expression<Func<TEntity, bool>>?> CaptureListPageFilter<TRepository, TEntity>(
            this Mock<TRepository> mock,
            Page<TEntity> pageToReturn)
            where TRepository : class, IBaseRepository<TEntity>
            where TEntity : class
        {
            Expression<Func<TEntity, bool>>? captured = null;

            mock.Setup(r => r.ListPageAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<Expression<Func<TEntity, bool>>>(),
                    It.IsAny<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<int, int, Expression<Func<TEntity, bool>>?, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>?, CancellationToken>(
                    (_, _, filter, _, _) => captured = filter)
                .ReturnsAsync(pageToReturn);

            return () => captured;
        }

        // Análogo a CaptureListPageFilter pero para el delegado de orderBy: lo intercepta,
        // lo aplica sobre una IQueryable en memoria (LINQ to Objects) y expone el resultado
        // ya ordenado. Sirve para verificar que un SortBy/SortDescending del PageRequest
        // efectivamente cambia el orden, y que la whitelist de columnas ordenables de cada
        // Service cae al orden por defecto ante un SortBy vacío o no reconocido.
        public static Func<IEnumerable<TEntity>, IEnumerable<TEntity>> CaptureListPageOrderBy<TRepository, TEntity>(
            this Mock<TRepository> mock,
            Page<TEntity> pageToReturn)
            where TRepository : class, IBaseRepository<TEntity>
            where TEntity : class
        {
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? captured = null;

            mock.Setup(r => r.ListPageAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<Expression<Func<TEntity, bool>>>(),
                    It.IsAny<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<int, int, Expression<Func<TEntity, bool>>?, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>?, CancellationToken>(
                    (_, _, _, orderBy, _) => captured = orderBy)
                .ReturnsAsync(pageToReturn);

            return items => captured is null ? items : captured(items.AsQueryable());
        }

        public static Page<TEntity> EmptyPage<TEntity>(int pageIndex = 0, int pageSize = 10) where TEntity : class => new()
        {
            Data = [],
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalRecords = 0
        };
    }
}
