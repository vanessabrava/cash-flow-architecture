using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CashFlowArchitecture.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgresSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "daily_balances",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    BalanceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalCredits = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDebits = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_balances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "financial_entries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EntryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financial_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "daily_balance_processed_events",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DailyBalanceId = table.Column<long>(type: "bigint", nullable: false),
                    EventUid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_balance_processed_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_daily_balance_processed_events_daily_balances_DailyBalanceId",
                        column: x => x.DailyBalanceId,
                        principalTable: "daily_balances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_daily_balance_processed_events_DailyBalanceId",
                table: "daily_balance_processed_events",
                column: "DailyBalanceId");

            migrationBuilder.CreateIndex(
                name: "IX_daily_balance_processed_events_EventUid",
                table: "daily_balance_processed_events",
                column: "EventUid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_daily_balances_BalanceDate",
                table: "daily_balances",
                column: "BalanceDate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_daily_balances_Uid",
                table: "daily_balances",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_financial_entries_EntryDate",
                table: "financial_entries",
                column: "EntryDate");

            migrationBuilder.CreateIndex(
                name: "IX_financial_entries_Uid",
                table: "financial_entries",
                column: "Uid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_balance_processed_events");

            migrationBuilder.DropTable(
                name: "financial_entries");

            migrationBuilder.DropTable(
                name: "daily_balances");
        }
    }
}
