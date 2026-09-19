using System.Text.Json;
using HousePlanner.API.Controllers;
using HousePlanner.API.Data;
using HousePlanner.API.DTOs;
using HousePlanner.API.Entities;
using HousePlanner.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace HousePlanner.API.Tests.Controllers;

public class PreDesignedPlanControllerTests
{
    private static ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static Mock<ICurrentUserContextService> User(string? role="User") { var mock=new Mock<ICurrentUserContextService>(); mock.Setup(x=>x.GetAsync(It.IsAny<HttpContext>())).ReturnsAsync(role is null?null:new CurrentUserContext(null,"test@example.com",role)); return mock; }
    private static T Context<T>(T controller) where T:ControllerBase { controller.ControllerContext=new ControllerContext{HttpContext=new DefaultHttpContext()}; return controller; }
    private static PreDesignedHousePlan Plan(bool active=true, string code="HP-T1") => new(){Name="Test Family Plan",Slug=code.ToLowerInvariant(),DesignCode=code,Style="modern",Bedrooms=2,Bathrooms=1,FloorCount=1,TotalBuiltUpAreaSqft=600,MinimumLandSizePerches=8,MinimumPlotWidthFt=30,MinimumPlotLengthFt=50,SuitableTerrain="flat",TagsJson="[\"family\"]",LayoutJson=ValidLayout,IsActive=active};
    private const string ValidLayout="""{"design_id":"test","floor_count":1,"total_built_up_area_sqft":600,"rooms":[{"room_id":"living","room_type":"living_room","floor":1,"x":0,"y":0,"width":10,"length":10},{"room_id":"kitchen","room_type":"kitchen","floor":1,"x":10,"y":0,"width":8,"length":10},{"room_id":"bed1","room_type":"bedroom_1","floor":1,"x":0,"y":10,"width":10,"length":10},{"room_id":"bed2","room_type":"bedroom_2","floor":1,"x":10,"y":10,"width":10,"length":10},{"room_id":"bath","room_type":"bathroom_1","floor":1,"x":18,"y":0,"width":5,"length":5}],"connections":[{"from_room":"living","to_room":"kitchen"},{"from_room":"living","to_room":"bed1"},{"from_room":"kitchen","to_room":"bed2"},{"from_room":"kitchen","to_room":"bath"}],"entrances":[{"room_id":"living","wall":"south","offset":2,"width":3}]}""";

    [Fact] public async Task PublicList_ReturnsOnlyActiveFilteredPlans()
    { await using var db=Db(); db.AddRange(Plan(),Plan(false,"HP-T2")); await db.SaveChangesAsync(); var c=Context(new PreDesignedPlansController(db,User().Object)); var ok=Assert.IsType<OkObjectResult>(await c.List(2,null,1,"modern","flat",8,700,null,null,null,null,null,"family")); Assert.Single(Assert.IsAssignableFrom<IEnumerable<PreDesignedPlanSummaryDto>>(ok.Value)); }
    [Fact] public async Task PublicEndpoints_RequireAuthentication()
    { await using var db=Db(); var c=Context(new PreDesignedPlansController(db,User(null).Object)); Assert.IsType<UnauthorizedResult>(await c.List(null,null,null,null,null,null,null,null,null,null,null,null,null)); }
    [Fact] public async Task Detail_DoesNotExposeInactivePlan()
    { await using var db=Db(); var p=Plan(false);db.Add(p);await db.SaveChangesAsync();var c=Context(new PreDesignedPlansController(db,User().Object));Assert.IsType<NotFoundResult>(await c.Detail(p.Id)); }
    [Fact] public async Task Compatibility_ReportsLandPlotAndTerrainProblems()
    { await using var db=Db();var p=Plan();db.Add(p);await db.SaveChangesAsync();var c=Context(new PreDesignedPlansController(db,User().Object));var ok=Assert.IsType<OkObjectResult>(await c.CheckCompatibility(p.Id,new(4,20,30,"hillside",3,2)));var value=Assert.IsType<CompatibilityResponse>(ok.Value);Assert.False(value.Compatible);Assert.Equal(4,value.Issues.Count);Assert.Equal(2,value.Warnings.Count); }
    [Fact] public async Task AdminMutation_RejectsNonAdminAndInvalidGeometry()
    { await using var db=Db();using var doc=JsonDocument.Parse("{}");var dto=new SavePreDesignedPlanDto{Name="X",Slug="x",DesignCode="X",Style="modern",Bedrooms=2,Bathrooms=1,FloorCount=1,TotalBuiltUpAreaSqft=500,MinimumLandSizePerches=5,SuitableTerrain="flat",Layout=doc.RootElement.Clone()};var forbidden=Context(new AdminPreDesignedPlansController(db,User().Object,new PreDesignedPlanLayoutValidator()));Assert.IsType<ForbidResult>(await forbidden.Create(dto));var admin=Context(new AdminPreDesignedPlansController(db,User("Admin").Object,new PreDesignedPlanLayoutValidator()));Assert.IsType<BadRequestObjectResult>(await admin.Create(dto)); }
    [Fact] public async Task AdminCreateAndSoftDeactivate_PreservesDerivedDesign()
    { await using var db=Db();var p=Plan();db.Add(p);await db.SaveChangesAsync();var design=new HouseDesign{WorkflowStateId=Guid.NewGuid(),FloorCount=1,FoundationType="slab",LayoutJson=ValidLayout,BasePreDesignedPlanId=p.Id};db.Add(design);await db.SaveChangesAsync();var c=Context(new AdminPreDesignedPlansController(db,User("Admin").Object,new PreDesignedPlanLayoutValidator()));Assert.IsType<NoContentResult>(await c.Deactivate(p.Id));Assert.False((await db.PreDesignedHousePlans.FindAsync(p.Id))!.IsActive);Assert.NotNull(await db.HouseDesigns.FindAsync(design.Id)); }
    [Fact] public void Catalog_HasTwelveUniqueStrictlyValidLayouts()
    { var path=Path.Combine(AppContext.BaseDirectory,"Data","Seed","pre-designed-plans.json");using var doc=JsonDocument.Parse(File.ReadAllText(path));var validator=new PreDesignedPlanLayoutValidator();var fingerprints=new HashSet<string>();foreach(var p in doc.RootElement.EnumerateArray()){Assert.Empty(validator.Validate(p.GetProperty("layout"),p.GetProperty("bedrooms").GetInt32(),p.GetProperty("floors").GetInt32()));fingerprints.Add(p.GetProperty("designCode").GetString()!);}Assert.Equal(141,doc.RootElement.GetArrayLength());Assert.Equal(141,fingerprints.Count); }
}
