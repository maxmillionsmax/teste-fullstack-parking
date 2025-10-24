using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Parking.Api.Data;
using Parking.Api.Dtos;
using Parking.Api.Models;
using Parking.Api.Services;

namespace Parking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VeiculosController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly PlacaService _placa;
        public VeiculosController(AppDbContext db, PlacaService placa) { _db = db; _placa = placa; }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] Guid? clienteId = null)
        {
            var q = _db.Veiculos.AsQueryable();
            if (clienteId.HasValue) q = q.Where(v => v.ClienteId == clienteId.Value);
            var list = await q.OrderBy(v => v.Placa).ToListAsync();
            return Ok(list);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] VeiculoCreateDto dto)
        {
            var placa = _placa.Sanitizar(dto.Placa);
            if (!_placa.EhValida(placa)) return BadRequest("Placa inválida.");
            if (await _db.Veiculos.AnyAsync(v => v.Placa == placa)) return Conflict("Placa já existe.");

            // create instance even if the parameterless constructor is non-public
            var v = (Veiculo)System.Activator.CreateInstance(typeof(Veiculo), nonPublic: true);
            v.Placa = placa;
            v.Modelo = dto.Modelo;
            v.Ano = dto.Ano;
            v.ClienteId = dto.ClienteId;
            _db.Veiculos.Add(v);
            await _db.SaveChangesAsync();

            // depois de salvar o veiculo v
            _db.VeiculosHistorico.Add(new VeiculoHistorico { VeiculoId = v.Id, ClienteId = v.ClienteId, Inicio = DateTime.UtcNow });
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = v.Id }, v);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var v = await _db.Veiculos.FindAsync(id);
            return v == null ? NotFound() : Ok(v);
        }

        // BUG propositado: não invalida/atualiza nada no front; candidato deve ajustar no front (React Query) ou aqui (retornar entidade e orientar)
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] VeiculoUpdateDto dto)
        {
            var v = await _db.Veiculos.FindAsync(id);
            if (v == null) return NotFound();
            var placa = _placa.Sanitizar(dto.Placa);
            if (!_placa.EhValida(placa)) return BadRequest("Placa inválida.");
            if (await _db.Veiculos.AnyAsync(x => x.Placa == placa && x.Id != id)) return Conflict("Placa já existe.");

            // atualizar placa/modelo/ano normalmente
            v.Placa = placa;
            v.Modelo = dto.Modelo;
            v.Ano = dto.Ano;

            // Se houver troca de cliente, fechar histórico anterior e criar novo histórico
            if (v.ClienteId != dto.ClienteId)
            {
                // fecha histórico anterior (se existir)
                var last = await _db.VeiculosHistorico
                    .Where(h => h.VeiculoId == v.Id && h.Fim == null)
                    .OrderByDescending(h => h.Inicio)
                    .FirstOrDefaultAsync();

                if (last != null)
                {
                    last.Fim = DateTime.UtcNow;
                }

                // cria novo histórico iniciando agora
                _db.VeiculosHistorico.Add(new VeiculoHistorico
                {
                    VeiculoId = v.Id,
                    ClienteId = dto.ClienteId,
                    Inicio = DateTime.UtcNow
                });

                v.ClienteId = dto.ClienteId;
            }

            await _db.SaveChangesAsync();
            return Ok(v);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var v = await _db.Veiculos.FindAsync(id);
            if (v == null) return NotFound();
            _db.Veiculos.Remove(v);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
