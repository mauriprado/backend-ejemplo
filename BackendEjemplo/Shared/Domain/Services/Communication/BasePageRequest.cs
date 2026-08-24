namespace BackendEjemplo.Shared.Domain.Services.Communication
{
    public class BasePageRequest
    {
        public int PageIndex { get; set; } = 0;
        public int PageSize { get; set; } = 10;

        // Nombre de columna por la que ordenar. Cada Service define su propia
        // whitelist de columnas ordenables (ver QueryableSortExtensions.ApplySort);
        // un valor no reconocido o vacío cae al orden por defecto de ese Service,
        // nunca lanza un error ni permite ordenar por una columna arbitraria.
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;
    }
}
