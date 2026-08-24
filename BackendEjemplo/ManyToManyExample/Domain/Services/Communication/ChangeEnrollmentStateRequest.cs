using BackendEjemplo.ManyToManyExample.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace BackendEjemplo.ManyToManyExample.Domain.Services.Communication
{
    public class ChangeEnrollmentStateRequest
    {
        [Required]
        public EnrollmentState State { get; set; }
    }
}
