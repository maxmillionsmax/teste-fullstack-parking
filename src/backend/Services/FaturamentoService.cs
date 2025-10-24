using Microsoft.EntityFrameworkCore;
using Parking.Api.Data;
using Parking.Api.Models;

namespace Parking.Api.Services
{
    public class FaturamentoService
    {
        private readonly AppDbContext _db;
        public FaturamentoService(AppDbContext db) => _db = db;

        // Gera faturas proporcionais considerando o histórico de associação dos veículos
        public async Task<List<Fatura>> GerarAsync(string competencia, CancellationToken ct = default)
        {
            // competencia formato yyyy-MM
            var part = competencia.Split('-');
            var ano = int.Parse(part[0]);
            var mes = int.Parse(part[1]);
            var ultimoDia = DateTime.DaysInMonth(ano, mes);
            var periodoInicio = new DateTime(ano, mes, 1).Date;
            var periodoFim = new DateTime(ano, mes, ultimoDia).Date;
            var diasNoMes = ultimoDia;

            // Carrega clientes mensalistas
            var mensalistas = await _db.Clientes
                .Where(c => c.Mensalista)
                .AsNoTracking()
                .ToListAsync(ct);

            // Idempotência: não recria faturas já existentes
            var criadas = new List<Fatura>();

            // Carrega veículos e históricos (em memória para cálculo)
            var veiculos = await _db.Veiculos.AsNoTracking().ToListAsync(ct);
            var historicos = await _db.VeiculosHistorico.AsNoTracking().ToListAsync(ct);

            // Para veículos sem histórico, cria um histórico implícito a partir de DataInclusao
            var historicosCompletos = new List<VeiculoHistorico>(historicos);
            foreach (var v in veiculos)
            {
                if (!historicos.Any(h => h.VeiculoId == v.Id))
                {
                    historicosCompletos.Add(new VeiculoHistorico
                    {
                        VeiculoId = v.Id,
                        ClienteId = v.ClienteId,
                        Inicio = v.DataInclusao,
                        Fim = null
                    });
                }
            }

            // Map clienteId => (valorTotalProporcional, setVeiculos)
            var resumoPorCliente = new Dictionary<Guid, (decimal total, HashSet<Guid> veiculos)>();

            foreach (var h in historicosCompletos)
            {
                // verifica se esse histórico intersecta o período
                var inicioHist = h.Inicio.Date;
                var fimHist = h.Fim?.Date ?? DateTime.MaxValue.Date;

                if (inicioHist > periodoFim) continue;
                if (fimHist < periodoInicio) continue;

                // overlap
                var overlapStart = inicioHist < periodoInicio ? periodoInicio : inicioHist;
                var overlapEnd = fimHist > periodoFim ? periodoFim : fimHist;

                if (overlapEnd < overlapStart) continue;

                var dias = (overlapEnd - overlapStart).Days + 1; // inclusivo

                // só soma se o cliente for mensalista
                var cliente = mensalistas.FirstOrDefault(c => c.Id == h.ClienteId);
                if (cliente == null) continue;

                var valorMens = cliente.ValorMensalidade ?? 0m;
                if (valorMens <= 0m) continue;

                var proporcao = (decimal)dias / diasNoMes;
                var contribuição = Math.Round(valorMens * proporcao, 2);

                if (!resumoPorCliente.TryGetValue(h.ClienteId, out var tuple))
                {
                    tuple = (0m, new HashSet<Guid>());
                }
                tuple.total += contribuição;
                tuple.veiculos.Add(h.VeiculoId);
                resumoPorCliente[h.ClienteId] = tuple;
            }

            // Cria faturas para cada cliente com total > 0 e que ainda não tenha fatura para a competencia
            foreach (var kv in resumoPorCliente)
            {
                var clienteId = kv.Key;
                var total = kv.Value.total;
                var veiculosSet = kv.Value.veiculos;

                if (total <= 0) continue;

                var jaExiste = await _db.Faturas.AnyAsync(f => f.ClienteId == clienteId && f.Competencia == competencia, ct);
                if (jaExiste) continue;

                var fat = new Fatura
                {
                    Competencia = competencia,
                    ClienteId = clienteId,
                    Valor = total,
                    Observacao = $"Proporcional - {diasNoMes} dias (gerado por histórico de associação)"
                };

                foreach (var vid in veiculosSet)
                    fat.Veiculos.Add(new FaturaVeiculo { FaturaId = fat.Id, VeiculoId = vid });

                _db.Faturas.Add(fat);
                criadas.Add(fat);
            }

            await _db.SaveChangesAsync(ct);
            return criadas;
        }
    }
}
