namespace BackendEjemplo.Shared.Domain.Services.Communication
{
    // Extiende BasePageRequest (PageIndex/PageSize) agregándole sorting — no lo
    // duplica, para que un cambio futuro en la paginación pura (ej. un campo nuevo
    // común a todo listado) no haya que replicarlo a mano en las dos clases.
    public class BaseSortPageRequest : BasePageRequest
    {
        // Nombre de columna por la que ordenar. Cada Service define su propia
        // whitelist de columnas ordenables (ver QueryableSortExtensions.ApplySort);
        // un valor no reconocido o vacío cae al orden por defecto de ese Service,
        // nunca lanza un error ni permite ordenar por una columna arbitraria.
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;
    }
}
