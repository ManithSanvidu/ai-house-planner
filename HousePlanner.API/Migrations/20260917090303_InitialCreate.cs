using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HousePlanner.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    RoleId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LandSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    BudgetLkr = table.Column<decimal>(type: "numeric(14,2)", nullable: true),
                    LandSizePerches = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    LandPhotoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ManualTerrainType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    PreferredBedrooms = table.Column<int>(type: "integer", nullable: false),
                    PreferredFloors = table.Column<int>(type: "integer", nullable: false),
                    StylePreference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BasePreDesignedPlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    PlanSelectionMode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LandSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LandSubmissions_PreDesignedHousePlans_BasePreDesignedPlanId",
                        column: x => x.BasePreDesignedPlanId,
                        principalTable: "PreDesignedHousePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LandSubmissions_Users_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LandSubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TerrainType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    SlopeEstimate = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    NotableFeatures = table.Column<string>(type: "jsonb", nullable: true),
                    ApprovalStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowStates_LandSubmissions_LandSubmissionId",
                        column: x => x.LandSubmissionId,
                        principalTable: "LandSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkflowStates_Users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HouseDesigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowStateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    FloorCount = table.Column<int>(type: "integer", nullable: false),
                    TotalBuiltUpAreaSqft = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    FoundationType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TemplateId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TerrainType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    LayoutJson = table.Column<string>(type: "jsonb", nullable: false),
                    DesignSource = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    BasePreDesignedPlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseDesigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HouseDesigns_PreDesignedHousePlans_BasePreDesignedPlanId",
                        column: x => x.BasePreDesignedPlanId,
                        principalTable: "PreDesignedHousePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HouseDesigns_WorkflowStates_WorkflowStateId",
                        column: x => x.WorkflowStateId,
                        principalTable: "WorkflowStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseDesignId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    X = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    Y = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    Width = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    Length = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    AreaSqft = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    WallHeight = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    FloorNumber = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rooms_HouseDesigns_HouseDesignId",
                        column: x => x.HouseDesignId,
                        principalTable: "HouseDesigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "User" },
                    { 2, "Admin" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_HouseDesigns_BasePreDesignedPlanId",
                table: "HouseDesigns",
                column: "BasePreDesignedPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseDesigns_WorkflowState_IsCurrent",
                table: "HouseDesigns",
                columns: new[] { "WorkflowStateId", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_HouseDesigns_WorkflowStateId",
                table: "HouseDesigns",
                column: "WorkflowStateId");

            migrationBuilder.CreateIndex(
                name: "UX_HouseDesigns_WorkflowState_Version",
                table: "HouseDesigns",
                columns: new[] { "WorkflowStateId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LandSubmissions_BasePreDesignedPlanId",
                table: "LandSubmissions",
                column: "BasePreDesignedPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_LandSubmissions_ClientId",
                table: "LandSubmissions",
                column: "ClientId");

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

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_HouseDesignId",
                table: "Rooms",
                column: "HouseDesignId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStates_ApprovedByUserId",
                table: "WorkflowStates",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStates_LandSubmissionId",
                table: "WorkflowStates",
                column: "LandSubmissionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Rooms");

            migrationBuilder.DropTable(
                name: "HouseDesigns");

            migrationBuilder.DropTable(
                name: "WorkflowStates");

            migrationBuilder.DropTable(
                name: "LandSubmissions");

            migrationBuilder.DropTable(
                name: "PreDesignedHousePlans");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
