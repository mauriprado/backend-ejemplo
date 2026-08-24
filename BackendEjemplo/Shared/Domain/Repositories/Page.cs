namespace BackendEjemplo.Shared.Domain.Repositories
{
    public class Page<TEntity> where TEntity : class
    {
        public IEnumerable<TEntity> Data { get; set; }
        public int PageIndex { get; set; }       // Página actual (índice base 0)
        public int PageSize { get; set; }        // Tamaño de página
        public int TotalRecords { get; set; }
    }
}
