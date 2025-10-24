using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Parking.Api.Data;
using Parking.Api.Models;
using Parking.Api.Services;
using System.Text;

namespace Parking.Api.Controllers
{
    [ApiController]
    [Route("api/import")]
    public class ImportController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly PlacaService _placa;
        public ImportController(AppDbContext db, PlacaService placa) { _db = db; _placa = placa; }

        private record ImportError(int Line, string Reason, string Raw);

        [HttpPost("csv")]
        public async Task<IActionResult> ImportCsv()
        {
            if (!Request.HasFormContentType || Request.Form.Files.Count == 0)
                return BadRequest("Envie um arquivo CSV no campo 'file'.");

            var file = Request.Form.Files[0];
            using var s = file.OpenReadStream();
            using var r = new StreamReader(s, Encoding.UTF8);

            int linha = 1; // header = linha 1
            int processados = 0, inseridos = 0;
            var erros = new List<ImportError>();

            var header = await r.ReadLineAsync(); // consome cabeçalho (linha 1)
            // Se não houver cabeçalho, header será null e linha do primeiro registro será 1 -> tratamos abaixo
            while (!r.EndOfStream)
            {
                var raw = await r.ReadLineAsync();
                linha++; // agora linha corresponde ao raw lido
                if (string.IsNullOrWhiteSpace(raw)) continue;
                processados++;

                // CSV simples separado por vírgula: placa,modelo,ano,cliente_identificador,cliente_nome,cliente_telefone,cliente_endereco,mensalista,valor_mensalidade
                string[] cols = raw.Split(',');
                try
                {
                    if (cols.Length < 9)
                    {
                        erros.Add(new ImportError(linha, $"Número insuficiente de colunas (esperado 9, encontrado {cols.Length})", raw));
                        continue;
                    }

                    var placaRaw = cols[0]?.Trim() ?? "";
                    var placa = _placa.Sanitizar(placaRaw);
                    var modelo = cols[1]?.Trim();
                    int? ano = int.TryParse(cols[2]?.Trim(), out var _ano) ? _ano : null;
                    var cliIdent = cols[3]?.Trim();
                    var cliNome = cols[4]?.Trim();
                    var cliTelRaw = cols[5] ?? "";
                    var cliTel = new string(cliTelRaw.Where(char.IsDigit).ToArray());
                    var cliEnd = cols[6]?.Trim();
                    var mensalistaStr = cols[7]?.Trim();
                    var valorMensStr = cols[8]?.Trim();

                    // validações
                    if (string.IsNullOrWhiteSpace(placaRaw))
                    {
                        erros.Add(new ImportError(linha, "Placa ausente", raw));
                        continue;
                    }

                    if (!_placa.EhValida(placa))
                    {
                        erros.Add(new ImportError(linha, "Placa inválida", raw));
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(mensalistaStr) && !bool.TryParse(mensalistaStr, out var _))
                    {
                        erros.Add(new ImportError(linha, $"Valor de 'mensalista' inválido: '{mensalistaStr}' (use true/false)", raw));
                        continue;
                    }
                    bool mensalista = bool.TryParse(mensalistaStr, out var mval) && mval;

                    decimal? valorMens = null;
                    if (!string.IsNullOrWhiteSpace(valorMensStr))
                    {
                        if (decimal.TryParse(valorMensStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var vm))
                        {
                            valorMens = vm;
                        }
                        else if (decimal.TryParse(valorMensStr, out vm))
                        {
                            valorMens = vm;
                        }
                        else
                        {
                            erros.Add(new ImportError(linha, $"Valor da mensalidade inválido: '{valorMensStr}'", raw));
                            continue;
                        }
                    }

                    // placa duplicada
                    if (await _db.Veiculos.AnyAsync(v => v.Placa == placa))
                    {
                        erros.Add(new ImportError(linha, "Placa duplicada", raw));
                        continue;
                    }

                    // buscar cliente por Nome+Telefone (convenção usada no import atual)
                    var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Nome == cliNome && c.Telefone == cliTel);
                    if (cliente == null)
                    {
                        cliente = new Cliente
                        {
                            Nome = cliNome ?? string.Empty,
                            Telefone = string.IsNullOrWhiteSpace(cliTel) ? null : cliTel,
                            Endereco = cliEnd,
                            Mensalista = mensalista,
                            ValorMensalidade = valorMens
                        };
                        _db.Clientes.Add(cliente);
                        try
                        {
                            await _db.SaveChangesAsync();
                        }
                        catch (DbUpdateException dbex)
                        {
                            // possível violação de unicidade ou outro problema ao criar cliente
                            erros.Add(new ImportError(linha, $"Falha ao criar cliente: {dbex.InnerException?.Message ?? dbex.Message}", raw));
                            // rejeita este registro e continua
                            // Descarta entrada do cliente do contexto para evitar efeitos colaterais
                            _db.Entry(cliente).State = EntityState.Detached;
                            continue;
                        }
                    }

                    var v = System.Activator.CreateInstance(typeof(Veiculo), nonPublic: true) as Veiculo;
                    if (v == null)
                    {
                        erros.Add(new ImportError(linha, "Falha ao criar instância de Veiculo", raw));
                        continue;
                    }
                    v.Placa = placa;
                    v.Modelo = modelo;
                    v.Ano = ano;
                    v.ClienteId = cliente.Id;
                    _db.Veiculos.Add(v);
                    try
                    {
                        await _db.SaveChangesAsync();
                        // adiciona histórico inicial para o veículo importado
                        _db.VeiculosHistorico.Add(new VeiculoHistorico
                        {
                            VeiculoId = v.Id,
                            ClienteId = cliente.Id,
                            Inicio = DateTime.UtcNow
                        });
                        await _db.SaveChangesAsync();

                        inseridos++;
                    }
                    catch (DbUpdateException dbex)
                    {
                        // por exemplo, violação de índice de placa se corrida concorrente criou a placa
                        erros.Add(new ImportError(linha, $"Falha ao inserir veículo: {dbex.InnerException?.Message ?? dbex.Message}", raw));
                        // desfaz o estado da entidade para evitar inconsistências
                        _db.Entry(v).State = EntityState.Detached;
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    // erro genérico ao processar a linha
                    erros.Add(new ImportError(linha, $"Erro processando linha: {ex.Message}", raw));
                    continue;
                }
            }

            return Ok(new { processados, inseridos, erros });
        }
    }
}
