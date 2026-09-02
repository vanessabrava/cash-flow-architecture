using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashFlowArchitecture.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UseSnakeCaseColumnNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_daily_balance_processed_events_daily_balances_DailyBalanceId",
                table: "daily_balance_processed_events");

            migrationBuilder.RenameColumn(
                name: "Uid",
                table: "financial_entries",
                newName: "uid");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "financial_entries",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "financial_entries",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "financial_entries",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "financial_entries",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "EntryDate",
                table: "financial_entries",
                newName: "entry_date");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "financial_entries",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_financial_entries_Uid",
                table: "financial_entries",
                newName: "IX_financial_entries_uid");

            migrationBuilder.RenameIndex(
                name: "IX_financial_entries_EntryDate",
                table: "financial_entries",
                newName: "IX_financial_entries_entry_date");

            migrationBuilder.RenameColumn(
                name: "Uid",
                table: "daily_balances",
                newName: "uid");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "daily_balances",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "daily_balances",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "daily_balances",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "TotalDebits",
                table: "daily_balances",
                newName: "total_debits");

            migrationBuilder.RenameColumn(
                name: "TotalCredits",
                table: "daily_balances",
                newName: "total_credits");

            migrationBuilder.RenameColumn(
                name: "BalanceDate",
                table: "daily_balances",
                newName: "balance_date");

            migrationBuilder.RenameIndex(
                name: "IX_daily_balances_Uid",
                table: "daily_balances",
                newName: "IX_daily_balances_uid");

            migrationBuilder.RenameIndex(
                name: "IX_daily_balances_BalanceDate",
                table: "daily_balances",
                newName: "IX_daily_balances_balance_date");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "daily_balance_processed_events",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "EventUid",
                table: "daily_balance_processed_events",
                newName: "event_uid");

            migrationBuilder.RenameColumn(
                name: "DailyBalanceId",
                table: "daily_balance_processed_events",
                newName: "daily_balance_id");

            migrationBuilder.RenameIndex(
                name: "IX_daily_balance_processed_events_EventUid",
                table: "daily_balance_processed_events",
                newName: "IX_daily_balance_processed_events_event_uid");

            migrationBuilder.RenameIndex(
                name: "IX_daily_balance_processed_events_DailyBalanceId",
                table: "daily_balance_processed_events",
                newName: "IX_daily_balance_processed_events_daily_balance_id");

            migrationBuilder.AddForeignKey(
                name: "FK_daily_balance_processed_events_daily_balances_daily_balance~",
                table: "daily_balance_processed_events",
                column: "daily_balance_id",
                principalTable: "daily_balances",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_daily_balance_processed_events_daily_balances_daily_balance~",
                table: "daily_balance_processed_events");

            migrationBuilder.RenameColumn(
                name: "uid",
                table: "financial_entries",
                newName: "Uid");

            migrationBuilder.RenameColumn(
                name: "type",
                table: "financial_entries",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "financial_entries",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "financial_entries",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "financial_entries",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "entry_date",
                table: "financial_entries",
                newName: "EntryDate");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "financial_entries",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_financial_entries_uid",
                table: "financial_entries",
                newName: "IX_financial_entries_Uid");

            migrationBuilder.RenameIndex(
                name: "IX_financial_entries_entry_date",
                table: "financial_entries",
                newName: "IX_financial_entries_EntryDate");

            migrationBuilder.RenameColumn(
                name: "uid",
                table: "daily_balances",
                newName: "Uid");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "daily_balances",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "daily_balances",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "daily_balances",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "total_debits",
                table: "daily_balances",
                newName: "TotalDebits");

            migrationBuilder.RenameColumn(
                name: "total_credits",
                table: "daily_balances",
                newName: "TotalCredits");

            migrationBuilder.RenameColumn(
                name: "balance_date",
                table: "daily_balances",
                newName: "BalanceDate");

            migrationBuilder.RenameIndex(
                name: "IX_daily_balances_uid",
                table: "daily_balances",
                newName: "IX_daily_balances_Uid");

            migrationBuilder.RenameIndex(
                name: "IX_daily_balances_balance_date",
                table: "daily_balances",
                newName: "IX_daily_balances_BalanceDate");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "daily_balance_processed_events",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "event_uid",
                table: "daily_balance_processed_events",
                newName: "EventUid");

            migrationBuilder.RenameColumn(
                name: "daily_balance_id",
                table: "daily_balance_processed_events",
                newName: "DailyBalanceId");

            migrationBuilder.RenameIndex(
                name: "IX_daily_balance_processed_events_event_uid",
                table: "daily_balance_processed_events",
                newName: "IX_daily_balance_processed_events_EventUid");

            migrationBuilder.RenameIndex(
                name: "IX_daily_balance_processed_events_daily_balance_id",
                table: "daily_balance_processed_events",
                newName: "IX_daily_balance_processed_events_DailyBalanceId");

            migrationBuilder.AddForeignKey(
                name: "FK_daily_balance_processed_events_daily_balances_DailyBalanceId",
                table: "daily_balance_processed_events",
                column: "DailyBalanceId",
                principalTable: "daily_balances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
