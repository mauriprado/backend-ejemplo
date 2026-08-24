namespace BackendEjemplo.Shared.Domain.Services.Communication
{
    public class BaseResponse<TEntity> where TEntity : class
    {

        public string Message { get; set; }
        public TEntity Content { get; set; }
        public bool Success { get; set; }

        // Distingue un fallo por "no existe" (404) de uno por conflicto de negocio,
        // como intentar borrar un recurso que tiene otros recursos dependientes (409).
        public bool IsConflict { get; set; }

        public BaseResponse(string message, bool isConflict = false)
        {
            Success = false;
            Message = message;
            Content = default;
            IsConflict = isConflict;
        }

        public BaseResponse(TEntity resource)
        {
            Success = true;
            Message = string.Empty;
            Content = resource;
        }
    }
}
