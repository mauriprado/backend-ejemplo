namespace BackendEjemplo.Shared.Domain.Services.Communication
{
    public class PageResponse<TEntity> where TEntity : class
    {
        public IEnumerable<TEntity> Data { get; set; } // Los datos paginados
        public int PageIndex { get; set; }       // Página actual (índice base 0)
        public int PageSize { get; set; }        // Tamaño de página
        public int TotalRecords { get; set; }   // Total de registros
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize); // Total de páginas calculado
    }
}
