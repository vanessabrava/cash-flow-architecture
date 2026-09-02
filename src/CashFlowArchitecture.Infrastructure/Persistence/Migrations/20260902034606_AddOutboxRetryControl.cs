using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashFlowArchitecture.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxRetryControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "failed_at",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "next_attempt_at",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_failed_at",
                table: "outbox_messages",
                column: "failed_at");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_next_attempt_at",
                table: "outbox_messages",
                column: "next_attempt_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_failed_at",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_next_attempt_at",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "failed_at",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "next_attempt_at",
                table: "outbox_messages");
        }
    }
}
