using System;
using System.ComponentModel.DataAnnotations;

namespace Parking.Api.Models
{
    public class VeiculoHistorico
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required] public Guid VeiculoId { get; set; }
        [Required] public Guid ClienteId { get; set; }
        [Required] public DateTime Inicio { get; set; } = DateTime.UtcNow;
        public DateTime? Fim { get; set; }

        public Veiculo? Veiculo { get; set; }
        public Cliente? Cliente { get; set; }
    }
}