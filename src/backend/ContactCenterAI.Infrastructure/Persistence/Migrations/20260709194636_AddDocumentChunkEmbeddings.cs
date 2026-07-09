using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace ContactCenterAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentChunkEmbeddings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EmbeddedAt",
                table: "document_chunks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Vector>(
                name: "Embedding",
                table: "document_chunks",
                type: "vector(1536)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmbeddingModel",
                table: "document_chunks",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmbeddedAt",
                table: "document_chunks");

            migrationBuilder.DropColumn(
                name: "Embedding",
                table: "document_chunks");

            migrationBuilder.DropColumn(
                name: "EmbeddingModel",
                table: "document_chunks");
        }
    }
}
