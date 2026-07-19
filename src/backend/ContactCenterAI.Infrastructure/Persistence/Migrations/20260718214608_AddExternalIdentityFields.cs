using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContactCenterAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalIdentityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthenticationProvider",
                table: "users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Local");

            migrationBuilder.AddColumn<string>(
                name: "ExternalSubject",
                table: "users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_ExternalSubject",
                table: "users",
                column: "ExternalSubject",
                unique: true,
                filter: "\"ExternalSubject\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_ExternalSubject",
                table: "users");

            migrationBuilder.DropColumn(
                name: "AuthenticationProvider",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ExternalSubject",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "users");
        }
    }
}
