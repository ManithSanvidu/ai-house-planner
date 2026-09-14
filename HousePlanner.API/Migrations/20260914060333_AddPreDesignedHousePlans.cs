using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HousePlanner.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPreDesignedHousePlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BasePreDesignedPlanId",
                table: "HouseDesigns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DesignSource",
                table: "HouseDesigns",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "ai_generated");

            migrationBuilder.CreateTable(
                name: "PreDesignedHousePlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Slug = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    DesignCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Style = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Bedrooms = table.Column<int>(type: "integer", nullable: false),
                    Bathrooms = table.Column<int>(type: "integer", nullable: false),
                    FloorCount = table.Column<int>(type: "integer", nullable: false),
                    TotalBuiltUpAreaSqft = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    MinimumLandSizePerches = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    MinimumPlotWidthFt = table.Column<decimal>(type: "numeric(8,2)", nullable: true),
                    MinimumPlotLengthFt = table.Column<decimal>(type: "numeric(8,2)", nullable: true),
                    SuitableTerrain = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ParkingSpaces = table.Column<int>(type: "integer", nullable: false),
                    HasBalcony = table.Column<bool>(type: "boolean", nullable: false),
                    HasVeranda = table.Column<bool>(type: "boolean", nullable: false),
                    HasOffice = table.Column<bool>(type: "boolean", nullable: false),
                    HasUtilityRoom = table.Column<bool>(type: "boolean", nullable: false),
                    IsAccessibleFriendly = table.Column<bool>(type: "boolean", nullable: false),
                    Category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    TagsJson = table.Column<string>(type: "jsonb", nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LayoutJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreDesignedHousePlans", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HouseDesigns_BasePreDesignedPlanId",
                table: "HouseDesigns",
                column: "BasePreDesignedPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PreDesignedHousePlans_Bathrooms",
                table: "PreDesignedHousePlans",
                column: "Bathrooms");

            migrationBuilder.CreateIndex(
                name: "IX_PreDesignedHousePlans_Bedrooms",
                table: "PreDesignedHousePlans",
                column: "Bedrooms");

            migrationBuilder.CreateIndex(
                name: "IX_PreDesignedHousePlans_DesignCode",
                table: "PreDesignedHousePlans",
                column: "DesignCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreDesignedHousePlans_FloorCount",
                table: "PreDesignedHousePlans",
                column: "FloorCount");

            migrationBuilder.CreateIndex(
                name: "IX_PreDesignedHousePlans_IsActive",
                table: "PreDesignedHousePlans",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PreDesignedHousePlans_Slug",
                table: "PreDesignedHousePlans",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreDesignedHousePlans_Style",
                table: "PreDesignedHousePlans",
                column: "Style");

            migrationBuilder.CreateIndex(
                name: "IX_PreDesignedHousePlans_SuitableTerrain",
                table: "PreDesignedHousePlans",
                column: "SuitableTerrain");

            migrationBuilder.AddForeignKey(
                name: "FK_HouseDesigns_PreDesignedHousePlans_BasePreDesignedPlanId",
                table: "HouseDesigns",
                column: "BasePreDesignedPlanId",
                principalTable: "PreDesignedHousePlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HouseDesigns_PreDesignedHousePlans_BasePreDesignedPlanId",
                table: "HouseDesigns");

            migrationBuilder.DropTable(
                name: "PreDesignedHousePlans");

            migrationBuilder.DropIndex(
                name: "IX_HouseDesigns_BasePreDesignedPlanId",
                table: "HouseDesigns");

            migrationBuilder.DropColumn(
                name: "BasePreDesignedPlanId",
                table: "HouseDesigns");

            migrationBuilder.DropColumn(
                name: "DesignSource",
                table: "HouseDesigns");

        }
    }
}
