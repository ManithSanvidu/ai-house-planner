using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using HousePlanner.API.Controllers;
using HousePlanner.API.Data;
using HousePlanner.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HousePlanner.API.Tests.Controllers;

/// <summary>
/// Tests for the full-ValidationResult persistence introduced in Step 1.
/// Covers: happy path (pass + fail), payload safety guards, legacy compatibility,
/// and that existing FailureReason / status behaviour is intact.
/// </summary>
public class ValidationResultPersistenceTests
{
    // ──────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────

    private static (ApplicationDbContext db, InternalWorkflowController ctrl) Build()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db   = new ApplicationDbContext(options);
        var ctrl = new InternalWorkflowController(db, new Mock<ILogger<InternalWorkflowController>>().Object);
        return (db, ctrl);
    }

    private static WorkflowState SeedWorkflow(ApplicationDbContext db, string status = "design_generated")
    {
        var w = new WorkflowState { Id = Guid.NewGuid(), LandSubmissionId = Guid.NewGuid(), Status = status };
        db.WorkflowStates.Add(w);
        db.SaveChanges();
        return w;
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement.Clone();

    // Two-rule PASS payload (mirrors the actual Python ValidationResult shape)
    private const string PassPayload = """
        {
          "passed": true,
          "rules": [
            {
              "rule_name": "coverage",
              "passed": true,
              "reason": "Ground coverage is 51.4% (1400.0 sqft), within maximum 65.00%.",
              "actual": "51.40% (1400.0 sqft)",
              "expected": "<= 65.00% (1772.0 sqft)"
            },
            {
              "rule_name": "preferences",
              "passed": true,
              "reason": "All preferences matched (Bedrooms: 3, Floors: 1).",
              "actual": "Bedrooms=3, Floors=1",
              "expected": "Bedrooms=3, Floors=1"
            }
          ],
          "errors": [],
          "summary": "Validation PASSED: Proposal satisfies all rules.",
          "revision_reason": null
        }
        """;

    // One-rule FAIL payload
    private const string FailPayload = """
        {
          "passed": false,
          "rules": [
            {
              "rule_name": "budget",
              "passed": false,
              "reason": "Cost LKR 6,000,000 exceeds budget LKR 4,500,000 by 33.3%.",
              "actual": "LKR 6,000,000 (+33.3% vs budget)",
              "expected": "<= LKR 4,950,000 (Budget + 10%)"
            }
          ],
          "errors": ["Cost LKR 6,000,000 exceeds budget LKR 4,500,000 by 33.3%."],
          "summary": "Validation FAILED: 1 rule(s) failed.",
          "revision_reason": "Cost LKR 6,000,000 exceeds budget LKR 4,500,000 by 33.3%."
        }
        """;

    // ──────────────────────────────────────────────────────────────
    // A. Full validation result persists (PASS)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task PassResult_PersistsFullJson_WithRulesAndExpectedActual()
    {
        var (db, ctrl) = Build();
        var workflow   = SeedWorkflow(db);

        var result = await ctrl.UpdateValidationStatus(workflow.Id, Parse(PassPayload));

        Assert.IsType<OkObjectResult>(result);

        var saved = await db.WorkflowStates.FindAsync(workflow.Id);
        Assert.NotNull(saved!.ValidationResultJson);

        using var doc  = JsonDocument.Parse(saved.ValidationResultJson!);
        var root       = doc.RootElement;

        // overall passed
        Assert.True(root.GetProperty("passed").GetBoolean());

        // rules array has two entries
        var rules = root.GetProperty("rules");
        Assert.Equal(JsonValueKind.Array, rules.ValueKind);
        Assert.Equal(2, rules.GetArrayLength());

        // first rule fields present
        var first = rules[0];
        Assert.Equal("coverage",       first.GetProperty("rule_name").GetString());
        Assert.True(first.GetProperty("passed").GetBoolean());
        Assert.Contains("51.4%",       first.GetProperty("actual").GetString());
        Assert.Contains("65.00%",      first.GetProperty("expected").GetString());
        Assert.False(string.IsNullOrEmpty(first.GetProperty("reason").GetString()));

        // summary preserved
        Assert.Contains("PASSED", root.GetProperty("summary").GetString());
    }

    // ──────────────────────────────────────────────────────────────
    // B. Failed validation — full result still persisted
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task FailResult_PersistsFullJson_WithFailedRuleEvidence()
    {
        var (db, ctrl) = Build();
        var workflow   = SeedWorkflow(db);

        var result = await ctrl.UpdateValidationStatus(workflow.Id, Parse(FailPayload));

        Assert.IsType<OkObjectResult>(result);

        var saved = await db.WorkflowStates.FindAsync(workflow.Id);
        Assert.NotNull(saved!.ValidationResultJson);

        using var doc = JsonDocument.Parse(saved.ValidationResultJson!);
        var root      = doc.RootElement;

        Assert.False(root.GetProperty("passed").GetBoolean());

        var rules = root.GetProperty("rules");
        Assert.Equal(1, rules.GetArrayLength());
        var rule = rules[0];
        Assert.Equal("budget",   rule.GetProperty("rule_name").GetString());
        Assert.False(rule.GetProperty("passed").GetBoolean());
        Assert.Contains("6,000,000", rule.GetProperty("actual").GetString());
        Assert.Contains("4,950,000", rule.GetProperty("expected").GetString());
    }

    // ──────────────────────────────────────────────────────────────
    // C. Legacy: existing workflow without ValidationResultJson works
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task LegacyWorkflow_ValidationResultJson_IsNullByDefault()
    {
        var (db, _) = Build();
        var workflow = SeedWorkflow(db);

        var saved = await db.WorkflowStates.FindAsync(workflow.Id);
        Assert.Null(saved!.ValidationResultJson); // not yet set → null safe
    }

    // ──────────────────────────────────────────────────────────────
    // D. Invalid / malformed payloads are rejected with 400
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task MissingPassedField_ReturnsBadRequest()
    {
        var (db, ctrl) = Build();
        SeedWorkflow(db);
        // `passed` omitted
        var bad = Parse(@"{ ""rules"": [], ""summary"": ""test"" }");
        var result = await ctrl.UpdateValidationStatus(Guid.NewGuid(), bad);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task PassedIsNotBoolean_ReturnsBadRequest()
    {
        var (db, ctrl) = Build();
        SeedWorkflow(db);
        var bad = Parse(@"{ ""passed"": ""yes"", ""rules"": [] }");
        var result = await ctrl.UpdateValidationStatus(Guid.NewGuid(), bad);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task RulesIsNotArray_ReturnsBadRequest()
    {
        var (db, ctrl) = Build();
        var w = SeedWorkflow(db);
        var bad = Parse(@"{ ""passed"": true, ""rules"": ""should-be-array"" }");
        var result = await ctrl.UpdateValidationStatus(w.Id, bad);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task OversizedPayload_ReturnsBadRequest()
    {
        var (db, ctrl) = Build();
        var w = SeedWorkflow(db);
        // Build a JSON string larger than 64 KB
        var hugeSummary = new string('x', 70_000);
        var huge = Parse($@"{{ ""passed"": true, ""rules"": [], ""summary"": ""{hugeSummary}"" }}");
        var result = await ctrl.UpdateValidationStatus(w.Id, huge);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UnknownWorkflow_ReturnsNotFound()
    {
        var (db, ctrl) = Build();
        var result = await ctrl.UpdateValidationStatus(Guid.NewGuid(), Parse(PassPayload));
        Assert.IsType<NotFoundObjectResult>(result);
        // Nothing persisted
        Assert.Empty(await db.WorkflowStates.ToListAsync());
    }

    // ──────────────────────────────────────────────────────────────
    // E. Existing status / FailureReason behaviour preserved
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task PassResult_SetsAwaitingApprovalStatus_ClearsFailureReason()
    {
        var (db, ctrl) = Build();
        var w = new WorkflowState
        {
            Id = Guid.NewGuid(),
            LandSubmissionId = Guid.NewGuid(),
            Status = "design_generated",
            FailureReason = "old failure"
        };
        db.WorkflowStates.Add(w);
        await db.SaveChangesAsync();

        await ctrl.UpdateValidationStatus(w.Id, Parse(PassPayload));

        var saved = await db.WorkflowStates.FindAsync(w.Id);
        Assert.Equal("awaiting_approval", saved!.Status);
        Assert.Equal("pending",           saved.ApprovalStatus);
        Assert.Null(saved.FailureReason);          // cleared on pass
        Assert.NotNull(saved.ValidationResultJson); // full evidence also stored
    }

    [Fact]
    public async Task FailResult_SetsFailureReason_FromSummary()
    {
        var (db, ctrl) = Build();
        var w = SeedWorkflow(db);

        await ctrl.UpdateValidationStatus(w.Id, Parse(FailPayload));

        var saved = await db.WorkflowStates.FindAsync(w.Id);
        // status not changed to awaiting_approval
        Assert.NotEqual("awaiting_approval", saved!.Status);
        // FailureReason set from summary (truncated to ≤ 1000 chars)
        Assert.NotNull(saved.FailureReason);
        Assert.Contains("FAILED", saved.FailureReason);
        // Full evidence still persisted
        Assert.NotNull(saved.ValidationResultJson);
    }

    [Fact]
    public async Task FailResult_FailureReason_TruncatedAt1000Chars()
    {
        var (db, ctrl) = Build();
        var w = SeedWorkflow(db);

        var longSummary = new string('A', 1500);
        var payload = Parse($@"{{
            ""passed"": false,
            ""rules"": [],
            ""summary"": ""{longSummary}"",
            ""revision_reason"": null
        }}");

        await ctrl.UpdateValidationStatus(w.Id, payload);

        var saved = await db.WorkflowStates.FindAsync(w.Id);
        Assert.NotNull(saved!.FailureReason);
        Assert.True(saved.FailureReason!.Length <= 1000);
    }

    // ──────────────────────────────────────────────────────────────
    // F. Null / missing optional fields are tolerated (actual/expected can vary)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task RuleWithNullActualAndExpected_IsStoredWithoutError()
    {
        var (db, ctrl) = Build();
        var w = SeedWorkflow(db);

        var payload = Parse(@"{
            ""passed"": true,
            ""rules"": [
              { ""rule_name"": ""budget"", ""passed"": true, ""reason"": ""Skipped."", ""actual"": null, ""expected"": null }
            ],
            ""errors"": [],
            ""summary"": ""Validation PASSED"",
            ""revision_reason"": null
        }");

        var result = await ctrl.UpdateValidationStatus(w.Id, payload);

        Assert.IsType<OkObjectResult>(result);

        var saved = await db.WorkflowStates.FindAsync(w.Id);
        Assert.NotNull(saved!.ValidationResultJson);

        using var doc  = JsonDocument.Parse(saved.ValidationResultJson!);
        var rule       = doc.RootElement.GetProperty("rules")[0];
        Assert.Equal(JsonValueKind.Null, rule.GetProperty("actual").ValueKind);
        Assert.Equal(JsonValueKind.Null, rule.GetProperty("expected").ValueKind);
    }
}
