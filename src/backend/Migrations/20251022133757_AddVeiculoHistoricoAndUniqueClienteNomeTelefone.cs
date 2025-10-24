using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parking.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVeiculoHistoricoAndUniqueClienteNomeTelefone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

            migrationBuilder.CreateTable(
                name: "cliente",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    endereco = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    mensalista = table.Column<bool>(type: "boolean", nullable: false),
                    valor_mensalidade = table.Column<decimal>(type: "numeric", nullable: true),
                    data_inclusao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cliente", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fatura",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    competencia = table.Column<string>(type: "text", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valor = table.Column<decimal>(type: "numeric", nullable: false),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    observacao = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fatura", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "veiculo",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    placa = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    modelo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ano = table.Column<int>(type: "integer", nullable: true),
                    data_inclusao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_veiculo", x => x.id);
                    table.ForeignKey(
                        name: "FK_veiculo_cliente_cliente_id",
                        column: x => x.cliente_id,
                        principalSchema: "public",
                        principalTable: "cliente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fatura_veiculo",
                schema: "public",
                columns: table => new
                {
                    fatura_id = table.Column<Guid>(type: "uuid", nullable: false),
                    veiculo_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fatura_veiculo", x => new { x.fatura_id, x.veiculo_id });
                    table.ForeignKey(
                        name: "FK_fatura_veiculo_fatura_fatura_id",
                        column: x => x.fatura_id,
                        principalSchema: "public",
                        principalTable: "fatura",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "veiculo_historico",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    veiculo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fim = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_veiculo_historico", x => x.id);
                    table.ForeignKey(
                        name: "FK_veiculo_historico_cliente_cliente_id",
                        column: x => x.cliente_id,
                        principalSchema: "public",
                        principalTable: "cliente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_veiculo_historico_veiculo_veiculo_id",
                        column: x => x.veiculo_id,
                        principalSchema: "public",
                        principalTable: "veiculo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cliente_nome_telefone",
                schema: "public",
                table: "cliente",
                columns: new[] { "nome", "telefone" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fatura_cliente_id_competencia",
                schema: "public",
                table: "fatura",
                columns: new[] { "cliente_id", "competencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_veiculo_cliente_id",
                schema: "public",
                table: "veiculo",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "IX_veiculo_placa",
                schema: "public",
                table: "veiculo",
                column: "placa",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_veiculo_historico_cliente_id",
                schema: "public",
                table: "veiculo_historico",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "IX_veiculo_historico_veiculo_id",
                schema: "public",
                table: "veiculo_historico",
                column: "veiculo_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fatura_veiculo",
                schema: "public");

            migrationBuilder.DropTable(
                name: "veiculo_historico",
                schema: "public");

            migrationBuilder.DropTable(
                name: "fatura",
                schema: "public");

            migrationBuilder.DropTable(
                name: "veiculo",
                schema: "public");

            migrationBuilder.DropTable(
                name: "cliente",
                schema: "public");
        }
    }
}
