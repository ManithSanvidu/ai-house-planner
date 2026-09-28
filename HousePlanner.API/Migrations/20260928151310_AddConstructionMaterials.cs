using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HousePlanner.API.Migrations
{
    /// <inheritdoc />
    public partial class AddConstructionMaterials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConstructionMaterials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    HouseDesignId = table.Column<Guid>(type: "uuid", nullable: true),
                    PhaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaterialName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RequiredQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AvailableQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    OrderedQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Supplier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ExpectedDeliveryDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConstructionMaterials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConstructionMaterials_ConstructionPhases_PhaseId",
                        column: x => x.PhaseId,
                        principalTable: "ConstructionPhases",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ConstructionMaterials_HouseDesigns_HouseDesignId",
                        column: x => x.HouseDesignId,
                        principalTable: "HouseDesigns",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ConstructionMaterials_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConstructionMaterials_HouseDesignId",
                table: "ConstructionMaterials",
                column: "HouseDesignId");

            migrationBuilder.CreateIndex(
                name: "IX_ConstructionMaterials_PhaseId",
                table: "ConstructionMaterials",
                column: "PhaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ConstructionMaterials_ProjectId",
                table: "ConstructionMaterials",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConstructionMaterials");
        }
    }
}
