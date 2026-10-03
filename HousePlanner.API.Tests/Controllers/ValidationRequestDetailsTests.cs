using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using HousePlanner.API.Controllers;
using HousePlanner.API.Data;
using HousePlanner.API.Entities;
using HousePlanner.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace HousePlanner.API.Tests.Controllers;

public class ValidationRequestDetailsTests
{
    private static (ApplicationDbContext db, ArchitectValidationRequestsController ctrl) Build()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new ApplicationDbContext(options);
        
        var userCtxMock = new Mock<ICurrentUserContextService>();
        userCtxMock.Setup(x => x.GetAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(new CurrentUserContext(Guid.NewGuid(), "architect@test.com", "Architect"));
            
        var ctrl = new ArchitectValidationRequestsController(db, userCtxMock.Object);
        var httpContext = new DefaultHttpContext();
        ctrl.ControllerContext = new ControllerContext { HttpContext = httpContext };
        
        return (db, ctrl);
    }

    private static async Task<ValidationRequest> SeedData(ApplicationDbContext db, string? validationJson, int bathrooms = 2, string? foundation = "Slab")
    {
        var client = new User { Id = Guid.NewGuid(), FullName = "Client", Email = "test@client.com", PasswordHash = "hash" };
        var ls = new LandSubmission { Id = Guid.NewGuid(), ClientId = client.Id, PreferredBathrooms = bathrooms };
        var hd = new HouseDesign { Id = Guid.NewGuid(), FoundationType = foundation, Version = 1 };
        var w = new WorkflowState 
        { 
            Id = Guid.NewGuid(), 
            LandSubmissionId = ls.Id, 
            LandSubmission = ls,
            HouseDesigns = new List<HouseDesign> { hd },
            ValidationResultJson = validationJson 
        };
        var req = new ValidationRequest 
        { 
            Id = Guid.NewGuid(), 
            ClientId = client.Id,
            Client = client,
            WorkflowStateId = w.Id, 
            WorkflowState = w, 
            HouseDesignId = hd.Id, 
            HouseDesign = hd, 
            Status = "Pending" 
        };
        
        db.Users.Add(client);
        db.LandSubmissions.Add(ls);
        db.HouseDesigns.Add(hd);
        db.WorkflowStates.Add(w);
        db.ValidationRequests.Add(req);
        await db.SaveChangesAsync();
        
        return req;
    }

    private static JsonElement ToJson(object obj) => JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(obj)).RootElement;

    [Fact]
    public async Task ArchitectEndpoint_ReturnsStructuredValidationResult_WhenJsonIsValid()
    {
        var (db, ctrl) = Build();
        var json = """
        {
          "passed": true,
          "rules": [
            {
              "rule_name": "coverage",
              "passed": true,
              "reason": "OK",
              "actual": "51.40%",
              "expected": "<= 65.00%"
            },
            {
              "rule_name": "preferences",
              "passed": true,
              "reason": "OK2",
              "actual": "Beds=3",
              "expected": "Beds=3"
            }
          ],
          "errors": [],
          "summary": "Validation PASSED",
          "revision_reason": null
        }
        """;
        var req = await SeedData(db, json);

        var result = await ctrl.GetRequestDetails(req.Id);
        var ok = Assert.IsType<OkObjectResult>(result);
        
        var dyn = ToJson(ok.Value!);
        Assert.True(dyn.TryGetProperty("validationResult", out var valRes) && valRes.ValueKind != System.Text.Json.JsonValueKind.Null);
        
        Assert.True(valRes.GetProperty("passed").GetBoolean());
        Assert.Equal("Validation PASSED", valRes.GetProperty("summary").GetString());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, valRes.GetProperty("revisionReason").ValueKind);
        
        var rules = valRes.GetProperty("rules");
        Assert.Equal(2, rules.GetArrayLength());
        
        var rule1 = rules[0];
        Assert.Equal("coverage", rule1.GetProperty("ruleName").GetString());
        Assert.True(rule1.GetProperty("passed").GetBoolean());
        Assert.Equal("OK", rule1.GetProperty("reason").GetString());
        Assert.Equal("51.40%", rule1.GetProperty("actual").GetString());
        Assert.Equal("<= 65.00%", rule1.GetProperty("expected").GetString());
    }

    [Fact]
    public async Task ArchitectEndpoint_ReturnsValidationResult_WhenFailed()
    {
        var (db, ctrl) = Build();
        var json = """
        {
          "passed": false,
          "rules": [
            {
              "rule_name": "budget",
              "passed": false,
              "reason": "Cost exceeds",
              "actual": 6000000,
              "expected": 4950000
            }
          ],
          "errors": ["Cost exceeds"],
          "summary": "Validation FAILED",
          "revision_reason": "Cost exceeds"
        }
        """;
        var req = await SeedData(db, json);

        var result = await ctrl.GetRequestDetails(req.Id);
        var ok = Assert.IsType<OkObjectResult>(result);
        
        var dyn = ToJson(ok.Value!);
        Assert.True(dyn.TryGetProperty("validationResult", out var valRes) && valRes.ValueKind != System.Text.Json.JsonValueKind.Null);
        
        Assert.False(valRes.GetProperty("passed").GetBoolean());
        
        var rules = valRes.GetProperty("rules");
        Assert.Equal(1, rules.GetArrayLength());
        
        var rule1 = rules[0];
        Assert.Equal("budget", rule1.GetProperty("ruleName").GetString());
        Assert.False(rule1.GetProperty("passed").GetBoolean());
        
        Assert.Equal(6000000m, rule1.GetProperty("actual").GetDecimal());
        Assert.Equal(4950000m, rule1.GetProperty("expected").GetDecimal());
    }

    [Fact]
    public async Task ArchitectEndpoint_ReturnsNullValidationResult_WhenLegacyWorkflow()
    {
        var (db, ctrl) = Build();
        var req = await SeedData(db, null);

        var result = await ctrl.GetRequestDetails(req.Id);
        var ok = Assert.IsType<OkObjectResult>(result);
        
        var dyn = ToJson(ok.Value!);
        Assert.True(dyn.TryGetProperty("validationResult", out var valRes) && valRes.ValueKind == System.Text.Json.JsonValueKind.Null);
        
        Assert.Equal(2, dyn.GetProperty("bathrooms").GetInt32());
        Assert.Equal("Slab", dyn.GetProperty("foundationType").GetString());
    }

    [Fact]
    public async Task ArchitectEndpoint_ReturnsNullValidationResult_WhenMalformedJson()
    {
        var (db, ctrl) = Build();
        var req = await SeedData(db, "{ malformed: json, ] }");

        var result = await ctrl.GetRequestDetails(req.Id);
        var ok = Assert.IsType<OkObjectResult>(result);
        
        var dyn = ToJson(ok.Value!);
        Assert.True(dyn.TryGetProperty("validationResult", out var valRes) && valRes.ValueKind == System.Text.Json.JsonValueKind.Null); // Should not crash
    }
}
