using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContactCenterAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(Persistence.ApplicationDbContext))]
    [Migration("20260719191000_AddTicketEscalationFields")]
    public partial class AddTicketEscalationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EscalationProcessedAt",
                table: "tickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EscalationStatus",
                table: "tickets",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EscalationProcessedAt",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "EscalationStatus",
                table: "tickets");
        }
    }
}
