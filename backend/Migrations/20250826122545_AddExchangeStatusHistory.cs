using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SkillForge.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddExchangeStatusHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExchangeStatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExchangeId = table.Column<int>(type: "integer", nullable: false),
                    FromStatus = table.Column<string>(type: "text", nullable: true),
                    ToStatus = table.Column<string>(type: "text", nullable: false),
                    ChangedBy = table.Column<int>(type: "integer", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'"),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExchangeStatusHistories_SkillExchanges_ExchangeId",
                        column: x => x.ExchangeId,
                        principalTable: "SkillExchanges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExchangeStatusHistories_Users_ChangedBy",
                        column: x => x.ChangedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeStatusHistory_ChangedBy",
                table: "ExchangeStatusHistories",
                column: "ChangedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeStatusHistory_ExchangeId_ChangedAt",
                table: "ExchangeStatusHistories",
                columns: new[] { "ExchangeId", "ChangedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExchangeStatusHistories");
        }
    }
}
