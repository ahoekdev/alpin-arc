using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class StagesCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stages",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    start_lodge_id = table.Column<long>(type: "bigint", nullable: false),
                    end_lodge_id = table.Column<long>(type: "bigint", nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    distance_meters = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stages", x => x.id);
                    table.ForeignKey(
                        name: "fk_stages_lodges_end_lodge_id",
                        column: x => x.end_lodge_id,
                        principalTable: "lodges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stages_lodges_start_lodge_id",
                        column: x => x.start_lodge_id,
                        principalTable: "lodges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_stages_end_lodge_id",
                table: "stages",
                column: "end_lodge_id");

            migrationBuilder.CreateIndex(
                name: "ix_stages_start_lodge_id_end_lodge_id",
                table: "stages",
                columns: new[] { "start_lodge_id", "end_lodge_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stages");
        }
    }
}
