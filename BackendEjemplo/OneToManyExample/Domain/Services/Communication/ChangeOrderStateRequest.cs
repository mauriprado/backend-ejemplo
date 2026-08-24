using BackendEjemplo.OneToManyExample.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace BackendEjemplo.OneToManyExample.Domain.Services.Communication
{
    public class ChangeOrderStateRequest
    {
        [Required]
        public OrderState State { get; set; }
    }
}
