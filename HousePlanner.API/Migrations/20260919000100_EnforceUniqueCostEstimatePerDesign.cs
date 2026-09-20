using HousePlanner.API.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HousePlanner.API.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260919000100_EnforceUniqueCostEstimatePerDesign")]
public class EnforceUniqueCostEstimatePerDesign : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM "CostEstimates" older
            USING "CostEstimates" newer
            WHERE older."HouseDesignId" = newer."HouseDesignId"
              AND (
                    older."CreatedAt" < newer."CreatedAt"
                    OR (older."CreatedAt" = newer."CreatedAt" AND older."Id" < newer."Id")
                  );
            """);

        migrationBuilder.DropIndex(
            name: "IX_CostEstimates_HouseDesignId",
            table: "CostEstimates");

        migrationBuilder.CreateIndex(
            name: "IX_CostEstimates_HouseDesignId",
            table: "CostEstimates",
            column: "HouseDesignId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_CostEstimates_HouseDesignId",
            table: "CostEstimates");

        migrationBuilder.CreateIndex(
            name: "IX_CostEstimates_HouseDesignId",
            table: "CostEstimates",
            column: "HouseDesignId");
    }
}
