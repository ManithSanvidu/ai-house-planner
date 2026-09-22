# Repository cleanup audit

**Final status: deletions rolled back.** Three backend tests fail with every candidate restored. Deleted files/folders remaining: **none**. Only this report is added; all pre-existing tracked file contents were verified unchanged. The REMOVE entries below record the initial proposal, not the final repository state.

Pre-deletion inspection completed on 2026-09-21. No files had been removed when this report was created.

## Evidence and scope

Inspected both csproj files, HousePlanner.slnx, package.json/lockfile, Vite configuration, imports, scripts, both CI workflows, tests, README/docs, and runtime configuration. Searched tracked and untracked repository text (including ignored configuration) for filenames and Python module imports; excluded dependency/build/cache trees, binary files, and files over 5 MB. No dynamic scratch-script discovery was found in agent app/scripts/CI. C# SDK implicit source inclusion and pytest discovery were considered separately from literal references. No secrets are reproduced here.

Modification times are filesystem times, not evidence of use. Git entries describe the last commit touching each file; purpose is inferred from file contents. No deletions are justified solely by age or filename. Uncertain manual tests/utilities are retained.

Existing changes in AiGenerationControllerTests.cs, CustomerConstructionLifecycleTests.cs, ConstructorWorkflowController.cs and two mobile Gradle cache files are preserved. No application code, dependencies, configuration, schema, database, migration, or test assertions will be changed.

## File candidates and reference checks

“No” means no reference found in that category in the inspected repository, not proof about external tools. Kept scripts can be useful manual entry points without references. Historical debug tests selected for deletion contain no collected test functions/assertions and only target plan IDs absent from both current data sources. Broad pytest can import these modules, but CI explicitly runs tests/; all actual test suites remain.

| Path | Type | Last modified; last Git change | Purpose / evidence | .csproj | .slnx | package.json | Imports | Scripts / deployment | Tests | Documentation | Runtime config | Decision / references |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `agentic-service/fix_library.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | One-time replacement of 4-bedroom seed records; retained guarded exporter is scripts/build_seed_catalogue.py. | No | No | No | No | No | No | No | No | REMOVE; None found |
| `agentic-service/fix_types.py` | py | 2026-09-12T17:46:43+05:30; 514b151 2026-09-12 Fix same-design bug, introduce geometry diversity, and correct workflow routing for React & ASP.NET | One-time text rewrite of Python union annotations in app/. | No | No | No | No | No | No | No | No | REMOVE; None found |
| `agentic-service/get_config.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Standalone configuration inspector; manual utility not proven obsolete. | No | No | No | No | No | No | No | No | KEEP; None found |
| `agentic-service/patch.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | One-time rewrite of a historical IDE scratch catalogue builder outside this repository. | No | No | No | No | No | No | No | No | REMOVE; None found |
| `agentic-service/patch_559.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Diagnostic edit for HP-4B1B-2F-559; references nonexistent app/design/seed/pre-designed-plans.json. | No | No | No | No | No | No | No | No | REMOVE; None found |
| `agentic-service/patch_adjacency.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | One-time string replacement in app/design/adjacency.py. | No | No | No | No | No | No | No | No | REMOVE; None found |
| `agentic-service/patch_all.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | One-time edits of bath, stair and bedroom dimensions in seed JSON. | No | No | No | No | No | No | No | No | REMOVE; None found |
| `agentic-service/patch_builder.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | One-time rewrite of room arrays in an external historical IDE scratch builder. | No | No | No | No | No | No | No | No | REMOVE; None found |
| `agentic-service/patch_builder2.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | One-time indentation and staircase rewrite in the external scratch builder. | No | No | No | No | No | No | No | No | REMOVE; None found |
| `agentic-service/patch_builder3.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | One-time line-number edit in the external scratch builder. | No | No | No | No | No | No | No | No | REMOVE; None found |
| `agentic-service/patch_builder4.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | One-time room width replacements in the external scratch builder. | No | No | No | No | No | No | No | No | REMOVE; None found |
| `agentic-service/patch_builder5.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | One-time bathroom coordinate replacement in the external scratch builder. | No | No | No | No | No | No | No | No | REMOVE; None found |
| `agentic-service/patch_json.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Print staircase dimensions for HP-4B1B-2F-559, absent from current seed and agent catalogues. | No | No | No | No | No | No | No | No | REMOVE; None found |
| `agentic-service/patch_quality.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | One-time injection of debug printing into architectural_quality.py. | No | No | No | No | No | No | No | No | REMOVE; None found |
| `agentic-service/patch_side_by_side.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | One-time seed JSON bath/hall dimension rewrite. | No | No | No | No | No | No | No | No | REMOVE; None found |
| `agentic-service/patch_stair.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | One-time seed JSON stair and bedroom dimension rewrite. | No | No | No | No | No | No | No | No | REMOVE; None found |
| `agentic-service/patch_test_design_models.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | One-time assertion tolerance replacement; replacement already present in retained test. | No | No | No | No | No | No | No | No | REMOVE; None found |
| `agentic-service/run_sample.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Manual seed sampling/validation utility; retained because ongoing usefulness is possible. | No | No | No | No | No | No | No | No | KEEP; None found |
| `agentic-service/scratch_debug.py` | py | 2026-09-18T14:18:10+05:30; d838952 2026-09-17 Implement token usage reduction and procedural fallback logic | Standalone fixed-input layout exception printer with no assertions or callers. | No | No | No | No | No | No | No | No | REMOVE; None found |
| `agentic-service/test_fail.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Manual diagnostic script; no external callers found, but obsolescence/duplicate coverage is not established; keep. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | KEEP; None found |
| `agentic-service/test_fail10.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Diagnostic for HP-4B1B-2F-559 or HP-4B1B-2F-561; neither ID exists in seed or agent catalogues; no assertions/test cases. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | REMOVE; None found |
| `agentic-service/test_fail11.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Diagnostic for HP-4B1B-2F-559 or HP-4B1B-2F-561; neither ID exists in seed or agent catalogues; no assertions/test cases. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | REMOVE; None found |
| `agentic-service/test_fail12.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Diagnostic for HP-4B1B-2F-559 or HP-4B1B-2F-561; neither ID exists in seed or agent catalogues; no assertions/test cases. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | REMOVE; None found |
| `agentic-service/test_fail13.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Diagnostic for HP-4B1B-2F-559 or HP-4B1B-2F-561; neither ID exists in seed or agent catalogues; no assertions/test cases. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | REMOVE; None found |
| `agentic-service/test_fail14.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Diagnostic for HP-4B1B-2F-559 or HP-4B1B-2F-561; neither ID exists in seed or agent catalogues; no assertions/test cases. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | REMOVE; None found |
| `agentic-service/test_fail15.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Manual diagnostic script; no external callers found, but obsolescence/duplicate coverage is not established; keep. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | KEEP; None found |
| `agentic-service/test_fail16.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Diagnostic for HP-4B1B-2F-559 or HP-4B1B-2F-561; neither ID exists in seed or agent catalogues; no assertions/test cases. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | REMOVE; None found |
| `agentic-service/test_fail17.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Manual diagnostic script; no external callers found, but obsolescence/duplicate coverage is not established; keep. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | KEEP; None found |
| `agentic-service/test_fail18.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Manual diagnostic script; no external callers found, but obsolescence/duplicate coverage is not established; keep. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | KEEP; None found |
| `agentic-service/test_fail19.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Manual diagnostic script; no external callers found, but obsolescence/duplicate coverage is not established; keep. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | KEEP; None found |
| `agentic-service/test_fail2.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Manual diagnostic script; no external callers found, but obsolescence/duplicate coverage is not established; keep. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | KEEP; None found |
| `agentic-service/test_fail20.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Manual diagnostic script; no external callers found, but obsolescence/duplicate coverage is not established; keep. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | KEEP; None found |
| `agentic-service/test_fail21.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Manual diagnostic script; no external callers found, but obsolescence/duplicate coverage is not established; keep. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | KEEP; None found |
| `agentic-service/test_fail22.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Diagnostic for HP-4B1B-2F-559 or HP-4B1B-2F-561; neither ID exists in seed or agent catalogues; no assertions/test cases. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | REMOVE; None found |
| `agentic-service/test_fail23.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Diagnostic for HP-4B1B-2F-559 or HP-4B1B-2F-561; neither ID exists in seed or agent catalogues; no assertions/test cases. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | REMOVE; None found |
| `agentic-service/test_fail24.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Manual diagnostic script; no external callers found, but obsolescence/duplicate coverage is not established; keep. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | KEEP; None found |
| `agentic-service/test_fail25.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Manual diagnostic script; no external callers found, but obsolescence/duplicate coverage is not established; keep. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | KEEP; None found |
| `agentic-service/test_fail26.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Manual diagnostic script; no external callers found, but obsolescence/duplicate coverage is not established; keep. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | KEEP; None found |
| `agentic-service/test_fail3.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Manual diagnostic script; no external callers found, but obsolescence/duplicate coverage is not established; keep. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | KEEP; None found |
| `agentic-service/test_fail4.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Diagnostic for HP-4B1B-2F-559 or HP-4B1B-2F-561; neither ID exists in seed or agent catalogues; no assertions/test cases. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | REMOVE; None found |
| `agentic-service/test_fail5.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Diagnostic for HP-4B1B-2F-559 or HP-4B1B-2F-561; neither ID exists in seed or agent catalogues; no assertions/test cases. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | REMOVE; None found |
| `agentic-service/test_fail6.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Diagnostic for HP-4B1B-2F-559 or HP-4B1B-2F-561; neither ID exists in seed or agent catalogues; no assertions/test cases. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | REMOVE; None found |
| `agentic-service/test_fail7.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Diagnostic for HP-4B1B-2F-559 or HP-4B1B-2F-561; neither ID exists in seed or agent catalogues; no assertions/test cases. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | REMOVE; None found |
| `agentic-service/test_fail8.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Manual diagnostic script; no external callers found, but obsolescence/duplicate coverage is not established; keep. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | KEEP; None found |
| `agentic-service/test_fail9.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Manual diagnostic script; no external callers found, but obsolescence/duplicate coverage is not established; keep. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | KEEP; None found |
| `agentic-service/test_filter3.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Manual diagnostic script; no external callers found, but obsolescence/duplicate coverage is not established; keep. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | KEEP; None found |
| `agentic-service/test_graph.py` | py | 2026-09-18T14:18:10+05:30; e66d6ce 2026-09-18 Fix catalogue generation regressions and update plan adapter | Manually invoked workflow integration demo; retained as integration-related. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | KEEP; None found |
| `agentic-service/test_load.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Manual diagnostic script; no external callers found, but obsolescence/duplicate coverage is not established; keep. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | KEEP; None found |
| `agentic-service/test_load2.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Manual diagnostic script; no external callers found, but obsolescence/duplicate coverage is not established; keep. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | KEEP; None found |
| `agentic-service/test_load3.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Manual diagnostic script; no external callers found, but obsolescence/duplicate coverage is not established; keep. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | KEEP; None found |
| `agentic-service/test_load4.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Manual diagnostic script; no external callers found, but obsolescence/duplicate coverage is not established; keep. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | KEEP; None found |
| `agentic-service/test_pz.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Manual diagnostic script; no external callers found, but obsolescence/duplicate coverage is not established; keep. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | KEEP; None found |
| `agentic-service/test_req.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Manual diagnostic script; no external callers found, but obsolescence/duplicate coverage is not established; keep. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | KEEP; None found |
| `agentic-service/test_rules2.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Manual diagnostic script; no external callers found, but obsolescence/duplicate coverage is not established; keep. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | KEEP; None found |
| `agentic-service/test_rules3.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Manual diagnostic script; no external callers found, but obsolescence/duplicate coverage is not established; keep. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | KEEP; None found |
| `agentic-service/test_tok2.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Manual diagnostic script; no external callers found, but obsolescence/duplicate coverage is not established; keep. | No | No | No | No | No | Implicit discovery by broad pytest; CI uses tests/ only | No | No | KEEP; None found |
| `agentic-service/verify_catalog.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Manual full-catalogue consistency audit; retained maintenance tool. | No | No | No | No | No | No | No | No | KEEP; None found |
| `agentic-service/verify_details.py` | py | 2026-09-18T22:34:22+05:30; a7a0879 2026-09-18 Update layout generation tool logic and dependecies | Manual catalogue overlap/count diagnostics; retained maintenance tool. | No | No | No | No | No | No | No | No | KEEP; None found |
| `agentic-service/scratch/remove_failing_plans.py` | py | 2026-09-19T18:32:42+05:30; d520394 2026-09-19 fix: fixed python ,Removed the 2 legacy PRO templates,Updated test_plan_suitability.py | One-time seed removal for two hardcoded plan codes; does not implement catalogue generation. | No | No | No | No | No | No | No | No | REMOVE; None found |
| `HousePlanner.API.Tests/test_output.txt` | txt | 2026-09-11T23:55:37+05:30; 505b344 2026-09-11 feat: complete UI overhaul and system design updates | Captured historical dotnet output, including an older dependency-conflict warning; not test input or source. | No | No | No | No | No | No | No | No | REMOVE; None found |
| `test_e2e.ps1` | ps1 | 2026-09-18T22:43:59+05:30; 17877d9 2026-09-18 feat: implement architecture validation request workflow | Manual E2E/lifecycle exercise; retained under explicit integration/lifecycle protection. test_phases scripts contain malformed assignments; not repaired in cleanup. | No | No | No | No | No | No | No | No | KEEP; None found |
| `test_phases.ps1` | ps1 | 2026-09-18T22:43:59+05:30; 17877d9 2026-09-18 feat: implement architecture validation request workflow | Manual E2E/lifecycle exercise; retained under explicit integration/lifecycle protection. test_phases scripts contain malformed assignments; not repaired in cleanup. | No | No | No | No | No | No | No | No | KEEP; None found |
| `test_phases2.ps1` | ps1 | 2026-09-18T22:43:59+05:30; 17877d9 2026-09-18 feat: implement architecture validation request workflow | Manual E2E/lifecycle exercise; retained under explicit integration/lifecycle protection. test_phases scripts contain malformed assignments; not repaired in cleanup. | No | No | No | No | No | No | No | No | KEEP; None found |

## Generated directories

| Path | Last modified | Type / proof | References / regeneration | Decision |
| --- | --- | --- | --- | --- |
| `HousePlanner.API/bin/` | 2026-09-20T14:42:39+05:30 | Compiled .NET assemblies/deps/runtimeconfig files; ignored and no tracked files. | SDK build/test output; generated from csproj via dotnet build/test. Solution includes source projects, not these directories. | REMOVE old output; regenerate during verification |
| `HousePlanner.API/obj/` | 2026-09-20T14:42:39+05:30 | NuGet assets/generated MSBuild props/targets and compilation intermediates; ignored and no tracked files. | SDK restore/build intermediates; recreated by dotnet restore/build. | REMOVE old output; regenerate during verification |
| `HousePlanner.API.Tests/bin/` | 2026-09-11T13:27:44+05:30 | Compiled .NET assemblies/deps/runtimeconfig files; ignored and no tracked files. | SDK build/test output; generated from csproj via dotnet build/test. Solution includes source projects, not these directories. | REMOVE old output; regenerate during verification |
| `HousePlanner.API.Tests/obj/` | 2026-09-20T14:05:46+05:30 | NuGet assets/generated MSBuild props/targets and compilation intermediates; ignored and no tracked files. | SDK restore/build intermediates; recreated by dotnet restore/build. | REMOVE old output; regenerate during verification |
| `HousePlanner-Web/node_modules/` | 2026-09-12T11:27:29+05:30 | npm dependency tree with .package-lock.json (349 packages), linked command wrappers; ignored, no tracked files. | package.json/lockfile, imports, Vite/TypeScript, tests, lint config/README require installed packages. npm install regenerates; remove only transactionally. | REMOVE old output; regenerate during verification |
| `HousePlanner-Web/dist/` | 2026-09-21T11:32:25+05:30 | Vite index.html, hashed assets, and copied public assets; ignored and no tracked files. | Output of package.json build script and input to vite preview; npm run build regenerates. Vite config has no custom source/output directory. | REMOVE old output; regenerate during verification |

## Other reviewed items kept

| Path | Type / purpose | References / reason retained |
| --- | --- | --- |
| `agentic-service/app/`, `requirements.txt`, `scripts/` | Agent runtime, schemas, configuration, catalogue data, and maintenance tools | Runtime imports; CI dependency installation; scripts/build_seed_catalogue.py is the retained exporter. Protected service modules. |
| `agentic-service/tests/`, `HousePlanner.API.Tests/`, `HousePlanner-Web/src/**/*.test.*` | Test suites | CI/pytest, csproj/solution SDK inclusion, Vitest discovery; all retained, including lifecycle/integration tests and UnitTest1.cs. Only the captured test_output.txt is a removable artifact. |
| `agentic-service/output_plans/` | Generated workflow renderings | rendering_agent.py writes workflow-specific images; external/database references cannot be excluded. Keep all images. |
| `agentic-service/execution_log.json` | Generated execution log | design_agent.py explicitly writes this runtime artifact; retained as requested for referenced files. |
| `.artifacts/` | Render samples/contact sheets | No source/config references found, but regeneration provenance and ongoing review use are not established; keep. |
| `README.md`, `HousePlanner-Web/README.md`, `docs/` | Setup/architecture documentation | Explicitly protected. No standalone temporary walkthrough/planning/report documents found beyond test_output.txt. |
| `HousePlanner.API/app.db`, `App_Data/`, `Migrations/`, seed data | Database/state/schema/catalogue | Runtime or persistent data; never remove on an unused-name assumption. |
| `HousePlanner.API/firebase-service-account.json`, `.env*`, appsettings and other config | Credentials/runtime configuration | README and application configuration; retain without exposing contents. |
| `HousePlanner-Web/src/`, `public/`, `package*.json`, `vite.config.ts`, tsconfig files | Frontend source/assets/build inputs | Imports/build/runtime; protected. Even apparently unused starter assets in src/ are retained. |
| `.venv/`, `agentic-service/venv/`, `.pytest_cache/`, `__pycache__/`, `.DS_Store` | Local environments/caches/OS metadata | Environments are in use and documented; these are outside the specifically selected cleanup set. |
| `mobile/`, `.github/`, `.gitignore`, `LICENSE`, `layout-schema.json` | Mobile project, CI, policy, license, contract | Outside removable scope or project inputs; preserve. |

## Verification and rollback plan

Move exactly the approved candidates to a private temporary backup outside the repository, preserving relative paths. Run dotnet restore, dotnet build, dotnet test at solution root; npm install, npm run build, npm test in HousePlanner-Web. Also run the retained agent tests because agent scripts were removed. Restore deletions if verification fails; resolve environment restrictions without changing application code. Preserve package-lock.json bytes if npm changes metadata. Keep reports and command logs outside runtime paths. Fresh generated build/dependency folders are expected after successful verification.

Rollback backup: `/private/tmp/houseplanner-cleanup-w9a5hbus`. Detailed deletion manifest: `/private/tmp/houseplanner-cleanup-w9a5hbus/manifest.json`.

## Final cleanup summary

**Deleted files: none.** All 31 candidate files and all six generated directories were restored after verification failed, as requested. The cleanup remains uncompleted because the restored baseline has three failing backend tests. No refactoring, behavior changes, assertion changes, or dependency manifest changes were made.

**Kept files:** all original files. In particular, preserve backend/frontend source, every test suite, integration/lifecycle scripts, migrations, data, credentials, agent runtime/configuration, maintenance tools, documentation, and runtime-generated plan images/logs. Individual keep reasons and proposed removal evidence appear above. Generated directories exist after rollback and verification; their contents may have been refreshed by the build tools.

| Component / command | Result | Evidence |
| --- | --- | --- |
| Backend: `dotnet restore` | PASS | Both solution projects restored/up to date. |
| Backend: `dotnet build` | PASS | 0 warnings, 0 errors. |
| Backend: `dotnet test` | FAIL | 133 passed, 3 failed, 136 total; every cleanup candidate was restored before this run. |
| Frontend: `npm install --cache <temporary-cache> --no-audit --no-fund` | PASS | Up to date; package.json and package-lock.json contents unchanged. |
| Frontend: `npm run build` | PASS | Vite production build; existing large-chunk warning only. |
| Frontend: `npm test` | PASS | 47 tests in 12 files. |
| Agent: `PYTHONPATH=<temporary-test-deps> venv/bin/python -m pytest tests/ -q` | PASS | 158 tests; 18 environment/deprecation warnings. |
| All tests overall | FAIL | Three backend test failures block a fully passing verification. |

Backend failures, all in `HousePlanner.API.Tests/Controllers/AiGenerationControllerTests.cs`:

- `Generate_IgnoresSpoofedClientIdAndUsesAuthenticatedCustomer` (line 73): stored client ID differs from the expected authenticated customer ID.
- `Generate_SelectedPlanIsRevalidatedBeforeWorkflowCreation` (line 161): expected `BadRequestObjectResult`, received `ObjectResult`.
- `Generate_CompatibleSelectedPlanReachesAiAsServerResolvedPlanCode` (line 182): expected `OkObjectResult`, received `ObjectResult`.

This test file already had user changes before cleanup. Repairing these failures would require a separate application/test change outside the authorized file-only cleanup. Tests were not weakened or skipped.

The first sandboxed npm attempt could not resolve registry.npmjs.org; the sandboxed dotnet restore stalled. These attempts were stopped and all deletions restored. Restore/install/build/test commands then ran successfully with the needed environment access except for the three application tests above. The first agent run could not import `responses`; it is already declared in requirements.txt, so it was installed with `--no-deps --target` into the temporary backup directory and supplied through PYTHONPATH. No virtual environment or requirements file was changed.

Agent tests rewrote execution_log.json; its original bytes were restored. After all commands completed, SHA-256 comparison found **zero tracked files differing from pre-cleanup contents**. All 37 candidate paths exist. Existing user edits remain intact.

### Paths temporarily removed and then restored

- `agentic-service/fix_library.py`
- `agentic-service/fix_types.py`
- `agentic-service/patch.py`
- `agentic-service/patch_559.py`
- `agentic-service/patch_adjacency.py`
- `agentic-service/patch_all.py`
- `agentic-service/patch_builder.py`
- `agentic-service/patch_builder2.py`
- `agentic-service/patch_builder3.py`
- `agentic-service/patch_builder4.py`
- `agentic-service/patch_builder5.py`
- `agentic-service/patch_json.py`
- `agentic-service/patch_quality.py`
- `agentic-service/patch_side_by_side.py`
- `agentic-service/patch_stair.py`
- `agentic-service/patch_test_design_models.py`
- `agentic-service/scratch_debug.py`
- `agentic-service/test_fail10.py`
- `agentic-service/test_fail11.py`
- `agentic-service/test_fail12.py`
- `agentic-service/test_fail13.py`
- `agentic-service/test_fail14.py`
- `agentic-service/test_fail16.py`
- `agentic-service/test_fail22.py`
- `agentic-service/test_fail23.py`
- `agentic-service/test_fail4.py`
- `agentic-service/test_fail5.py`
- `agentic-service/test_fail6.py`
- `agentic-service/test_fail7.py`
- `agentic-service/scratch/remove_failing_plans.py`
- `HousePlanner.API.Tests/test_output.txt`
- `HousePlanner.API/bin/`
- `HousePlanner.API/obj/`
- `HousePlanner.API.Tests/bin/`
- `HousePlanner.API.Tests/obj/`
- `HousePlanner-Web/node_modules/`
- `HousePlanner-Web/dist/`

### Verification logs

Logs and the original-file backup are outside the repository at `/private/tmp/houseplanner-cleanup-w9a5hbus/`:

- `dotnet-restore-baseline.log`, `dotnet-build-baseline.log`, `dotnet-test-baseline.log`
- `npm-install-verified.log`, `npm-build-verified.log`, `npm-test-verified.log`
- `agent-tests-baseline.log`, `pip-responses.log`
- `manifest.json`, `initial-status.txt`, `initial-hashes.json`, `integrity-result.json`

The files labelled baseline were run after rollback. Temporary backups may be removed by the operating system; the report is the durable record.
