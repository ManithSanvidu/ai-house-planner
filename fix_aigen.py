import sys

with open("HousePlanner.API/Controllers/AiGenerationController.cs", "r") as f:
    content = f.read()

target1 = """                PreDesignedHousePlan? basePlan = null;
                if (request.BasePreDesignedPlanId.HasValue)
                {
                    basePlan = await _context.PreDesignedHousePlans.FirstOrDefaultAsync(p => p.Id == request.BasePreDesignedPlanId && p.IsActive);
                    if (basePlan is null) return BadRequest(new { Message = "The selected pre-designed plan is unavailable." });
                }"""

replacement1 = """                PreDesignedHousePlan? basePlan = null;
                if (request.BasePreDesignedPlanId.HasValue)
                {
                    basePlan = await _context.PreDesignedHousePlans.FirstOrDefaultAsync(p => p.Id == request.BasePreDesignedPlanId && p.IsActive);
                    if (basePlan is null) return BadRequest(new { Message = "The selected pre-designed plan is unavailable." });
                    
                    var specificValidation = await _designOptionsService.ValidateSpecificPlanAsync(basePlan, request, cancellationToken);
                    if (!specificValidation.IsValid)
                    {
                        return BadRequest(new { 
                            code = specificValidation.ErrorCode, 
                            message = specificValidation.Message,
                            conflicts = specificValidation.Conflicts,
                            suggestions = specificValidation.Suggestions 
                        });
                    }
                }"""

target2 = """                    ,base_pre_designed_plan_id = request.BasePreDesignedPlanId
                    ,plan_selection_mode = request.PlanSelectionMode
                };"""

replacement2 = """                    ,base_pre_designed_plan_id = request.BasePreDesignedPlanId
                    ,plan_selection_mode = request.PlanSelectionMode
                    ,preferred_plan_code = basePlan?.DesignCode
                };"""

if target1 in content:
    content = content.replace(target1, replacement1)
    print("Replaced target 1")
else:
    print("Target 1 not found")

if target2 in content:
    content = content.replace(target2, replacement2)
    print("Replaced target 2")
else:
    print("Target 2 not found")

with open("HousePlanner.API/Controllers/AiGenerationController.cs", "w") as f:
    f.write(content)
