using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddToursMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "tours",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "citext", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tours", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tour_variants",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tour_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "citext", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tour_variants", x => x.id);
                    table.ForeignKey(
                        name: "fk_tour_variants_tours_tour_id",
                        column: x => x.tour_id,
                        principalTable: "tours",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tour_variant_stages",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tour_variant_id = table.Column<long>(type: "bigint", nullable: false),
                    stage_id = table.Column<long>(type: "bigint", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tour_variant_stages", x => x.id);
                    table.CheckConstraint("ck_tour_variant_stages_order_positive", "\"order\" >= 1");
                    table.ForeignKey(
                        name: "fk_tour_variant_stages_stages_stage_id",
                        column: x => x.stage_id,
                        principalTable: "stages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tour_variant_stages_tour_variants_tour_variant_id",
                        column: x => x.tour_variant_id,
                        principalTable: "tour_variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tour_variant_stages_stage_id",
                table: "tour_variant_stages",
                column: "stage_id");

            migrationBuilder.CreateIndex(
                name: "ix_tour_variant_stages_tour_variant_id_order",
                table: "tour_variant_stages",
                columns: new[] { "tour_variant_id", "order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tour_variants_tour_id_name",
                table: "tour_variants",
                columns: new[] { "tour_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tours_name",
                table: "tours",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tour_variant_stages");

            migrationBuilder.DropTable(
                name: "tour_variants");

            migrationBuilder.DropTable(
                name: "tours");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");
        }
    }
}
