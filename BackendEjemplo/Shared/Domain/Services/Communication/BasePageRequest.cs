namespace BackendEjemplo.Shared.Domain.Services.Communication
{
    public class BasePageRequest
    {
        public int PageIndex { get; set; } = 0;
        public int PageSize { get; set; } = 10;
    }
}
