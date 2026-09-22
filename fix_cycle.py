import sys

def replace_in_file(filepath, targets, replacements):
    with open(filepath, 'r') as f:
        content = f.read()
    
    for i in range(len(targets)):
        if targets[i] in content:
            content = content.replace(targets[i], replacements[i])
        else:
            print(f"Target {i+1} not found in {filepath}!")
            return False
            
    with open(filepath, 'w') as f:
        f.write(content)
    return True

# 1. IConstructorWorkflowService.cs
i_svc_file = "HousePlanner.API/Services/IConstructorWorkflowService.cs"
i_svc_target = """Task<IEnumerable<Project>> GetConstructorProjectsAsync(Guid constructorId, string userRole);"""
i_svc_replacement = """Task<IEnumerable<HousePlanner.API.DTOs.ConstructorProjectDto>> GetConstructorProjectsAsync(Guid constructorId, string userRole);
        Task<Project?> GetProjectEntityAsync(Guid projectId, Guid constructorId, string userRole);"""

if replace_in_file(i_svc_file, [i_svc_target], [i_svc_replacement]):
    print("Updated IConstructorWorkflowService.cs")

# 2. ConstructorWorkflowService.cs
svc_file = "HousePlanner.API/Services/ConstructorWorkflowService.cs"

# Replace GetConstructorProjectsAsync implementation
svc_target1 = """        public async Task<IEnumerable<Project>> GetConstructorProjectsAsync(Guid constructorId, string userRole)
        {
            var query = _context.Projects.Include(p => p.ConstructionPhases).AsQueryable();

            if (!string.Equals(userRole, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.ContractorId == constructorId);
            }

            return await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        }"""

svc_replacement1 = """        public async Task<Project?> GetProjectEntityAsync(Guid projectId, Guid constructorId, string userRole)
        {
            var query = _context.Projects.Include(p => p.ConstructionPhases).AsQueryable();
            if (!string.Equals(userRole, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.ContractorId == constructorId);
            }
            return await query.FirstOrDefaultAsync(p => p.Id == projectId);
        }

        public async Task<IEnumerable<HousePlanner.API.DTOs.ConstructorProjectDto>> GetConstructorProjectsAsync(Guid constructorId, string userRole)
        {
            var query = _context.Projects.AsQueryable();

            if (!string.Equals(userRole, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.ContractorId == constructorId);
            }

            return await query.OrderByDescending(p => p.CreatedAt)
                .Select(p => new HousePlanner.API.DTOs.ConstructorProjectDto(
                    p.Id,
                    p.WorkflowStateId,
                    p.HouseDesignId,
                    p.ContractorId,
                    p.Status,
                    p.CreatedAt,
                    p.UpdatedAt,
                    p.ConstructionPhases.OrderBy(cp => cp.SequenceOrder).Select(cp => new HousePlanner.API.DTOs.ConstructionPhaseDto(
                        cp.Id,
                        cp.PhaseName,
                        cp.SequenceOrder,
                        cp.Status,
                        cp.StartedAt,
                        cp.CompletedAt,
                        cp.EstimatedDurationDays
                    )).ToList()
                ))
                .ToListAsync();
        }"""

# Replace GetProjectDetailsAsync implementation (used GetConstructorProjectsAsync previously)
svc_target2 = """        public async Task<Project?> GetProjectDetailsAsync(Guid projectId, Guid constructorId, string userRole)
        {
            var projects = await GetConstructorProjectsAsync(constructorId, userRole);
            var project = projects.FirstOrDefault(p => p.Id == projectId);
            
            if (project != null)
            {
                project.ConstructionPhases = project.ConstructionPhases.OrderBy(c => c.SequenceOrder).ToList();
            }

            return project;
        }"""

svc_replacement2 = """        public async Task<Project?> GetProjectDetailsAsync(Guid projectId, Guid constructorId, string userRole)
        {
            var project = await GetProjectEntityAsync(projectId, constructorId, userRole);
            
            if (project != null)
            {
                project.ConstructionPhases = project.ConstructionPhases.OrderBy(c => c.SequenceOrder).ToList();
            }

            return project;
        }"""

if replace_in_file(svc_file, [svc_target1, svc_target2], [svc_replacement1, svc_replacement2]):
    print("Updated ConstructorWorkflowService.cs")

# 3. ConstructorWorkflowController.cs
# We also need to map the single GetProjectDetailsAsync endpoint to a DTO, otherwise it will cycle too!
# Let's check ConstructorWorkflowController.cs

ctrl_file = "HousePlanner.API/Controllers/ConstructorWorkflowController.cs"
ctrl_target1 = """        public async Task<IActionResult> GetProjectDetails(Guid projectId)
        {
            var user = await _currentUserContext.GetAsync(HttpContext);
            if (user?.Id == null) return Unauthorized();

            var project = await _workflowService.GetProjectDetailsAsync(projectId, user.Id.Value, user.Role);
            if (project == null) return NotFound("Project not found or unauthorized.");

            return Ok(project);
        }"""

ctrl_replacement1 = """        public async Task<IActionResult> GetProjectDetails(Guid projectId)
        {
            var user = await _currentUserContext.GetAsync(HttpContext);
            if (user?.Id == null) return Unauthorized();

            var p = await _workflowService.GetProjectDetailsAsync(projectId, user.Id.Value, user.Role);
            if (p == null) return NotFound("Project not found or unauthorized.");

            var dto = new HousePlanner.API.DTOs.ConstructorProjectDto(
                p.Id,
                p.WorkflowStateId,
                p.HouseDesignId,
                p.ContractorId,
                p.Status,
                p.CreatedAt,
                p.UpdatedAt,
                p.ConstructionPhases.Select(cp => new HousePlanner.API.DTOs.ConstructionPhaseDto(
                    cp.Id,
                    cp.PhaseName,
                    cp.SequenceOrder,
                    cp.Status,
                    cp.StartedAt,
                    cp.CompletedAt,
                    cp.EstimatedDurationDays
                )).ToList()
            );

            return Ok(dto);
        }"""

if replace_in_file(ctrl_file, [ctrl_target1], [ctrl_replacement1]):
    print("Updated ConstructorWorkflowController.cs")

