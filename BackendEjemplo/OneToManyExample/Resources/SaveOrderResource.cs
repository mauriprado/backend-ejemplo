using System.ComponentModel.DataAnnotations;

namespace BackendEjemplo.OneToManyExample.Resources
{
    public class SaveOrderResource
    {
        [Required]
        public Decimal? TotalAmount { get; set; }
        [Required]
        public long? ClientId { get; set; }
    }
}
