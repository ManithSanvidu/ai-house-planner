using HousePlanner.API.Data;
using HousePlanner.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace HousePlanner.API.Services
{
    public class ConstructorWorkflowService : IConstructorWorkflowService
    {
        private readonly ApplicationDbContext _context;

        public ConstructorWorkflowService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Project?> GetProjectEntityAsync(Guid projectId, Guid constructorId, string userRole)
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
        }

        public async Task<Project?> GetProjectDetailsAsync(Guid projectId, Guid constructorId, string userRole)
        {
            var project = await GetProjectEntityAsync(projectId, constructorId, userRole);
            
            if (project != null)
            {
                project.ConstructionPhases = project.ConstructionPhases.OrderBy(c => c.SequenceOrder).ToList();
            }

            return project;
        }

        public async Task<IEnumerable<ConstructorWorkflowLog>> GetWorkflowLogsAsync(Guid projectId, Guid constructorId, string userRole)
        {
            var query = _context.ConstructorWorkflowLogs
                .Include(l => l.ConstructionPhase)
                .Where(l => l.ProjectId == projectId);

            if (!string.Equals(userRole, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(l => l.ConstructorId == constructorId);
            }

            return await query
                .OrderByDescending(l => l.Date)
                .ThenByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        public async Task<ConstructorWorkflowLog> CreateWorkflowLogAsync(Guid constructorId, ConstructorWorkflowLog log)
        {
            // Verify project access
            var project = await GetProjectDetailsAsync(log.ProjectId, constructorId, "Constructor");
            if (project == null) throw new UnauthorizedAccessException("Not authorized to log workflow for this project.");

            log.ConstructorId = constructorId;
            log.CreatedAt = DateTimeOffset.UtcNow;
            log.UpdatedAt = DateTimeOffset.UtcNow;

            if (log.DayNumber <= 0)
            {
                var previousLogs = await _context.ConstructorWorkflowLogs
                    .Where(l => l.ProjectId == log.ProjectId)
                    .OrderByDescending(l => l.DayNumber)
                    .FirstOrDefaultAsync();
                
                log.DayNumber = previousLogs != null ? previousLogs.DayNumber + 1 : 1;
            }

            _context.ConstructorWorkflowLogs.Add(log);
            await _context.SaveChangesAsync();

            return log;
        }

        public async Task<ConstructorWorkflowLog> UpdateWorkflowLogAsync(Guid constructorId, Guid logId, ConstructorWorkflowLog updatedLog)
        {
            var existingLog = await _context.ConstructorWorkflowLogs
                .FirstOrDefaultAsync(l => l.Id == logId && l.ConstructorId == constructorId);

            if (existingLog == null) throw new KeyNotFoundException("Log not found or unauthorized.");

            existingLog.CompletedWork = updatedLog.CompletedWork;
            existingLog.ProgressPercentage = updatedLog.ProgressPercentage;
            existingLog.Challenges = updatedLog.Challenges;
            existingLog.Issues = updatedLog.Issues;
            existingLog.Resolution = updatedLog.Resolution;
            existingLog.TomorrowPlan = updatedLog.TomorrowPlan;
            existingLog.AdditionalNotes = updatedLog.AdditionalNotes;
            existingLog.Status = updatedLog.Status;
            
            if (updatedLog.ConstructionPhaseId.HasValue && updatedLog.ConstructionPhaseId != Guid.Empty)
            {
                existingLog.ConstructionPhaseId = updatedLog.ConstructionPhaseId;
            }

            existingLog.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return existingLog;
        }

        public async Task<object> GetProjectProgressAsync(Guid projectId, Guid constructorId, string userRole)
        {
            var project = await GetProjectDetailsAsync(projectId, constructorId, userRole);
            if (project == null) throw new UnauthorizedAccessException("Not authorized.");

            int totalEstimatedDays = project.ConstructionPhases.Sum(p => p.EstimatedDurationDays);
            
            var logs = await GetWorkflowLogsAsync(projectId, constructorId, userRole);
            int daysCompleted = logs.Count(); // Each log is one day
            
            int daysRemaining = totalEstimatedDays - daysCompleted;
            if (daysRemaining < 0) daysRemaining = 0;

            int overallProgress = 0;
            if (totalEstimatedDays > 0)
            {
                overallProgress = (int)Math.Min(100, Math.Round((double)daysCompleted / totalEstimatedDays * 100));
            }

            var latestLog = logs.FirstOrDefault();
            
            string delayStatus = "On Schedule";
            if (daysCompleted > totalEstimatedDays)
            {
                delayStatus = $"Delayed by {daysCompleted - totalEstimatedDays} days";
            }
            
            return new
            {
                TotalEstimatedDays = totalEstimatedDays,
                DaysCompleted = daysCompleted,
                DaysRemaining = daysRemaining,
                OverallProgress = overallProgress,
                DelayStatus = delayStatus,
                LatestLog = latestLog
            };
        }
        public async Task<Project?> SearchProjectByIdAsync(Guid projectId)
        {
            return await _context.Projects
                .Include(p => p.WorkflowState)
                .ThenInclude(w => w.HouseDesigns)
                .FirstOrDefaultAsync(p => p.Id == projectId);
        }

        public async Task<ConstructorProjectRequest> RequestProjectAssignmentAsync(Guid projectId, Guid constructorId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null) throw new KeyNotFoundException("Project not found.");

            // Check if already requested
            var existingRequest = await _context.ConstructorProjectRequests
                .FirstOrDefaultAsync(r => r.ProjectId == projectId && r.ConstructorId == constructorId);

            if (existingRequest != null)
            {
                return existingRequest;
            }

            var request = new ConstructorProjectRequest
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                ConstructorId = constructorId,
                Status = "Pending"
            };

            _context.ConstructorProjectRequests.Add(request);
            await _context.SaveChangesAsync();

            return request;
        }

        public async Task<ConstructorProjectRequest> ApproveConstructorRequestAsync(Guid requestId, Guid ownerId)
        {
            var request = await _context.ConstructorProjectRequests
                .Include(r => r.Project)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null) throw new KeyNotFoundException("Request not found.");
            if (request.Project == null) throw new InvalidOperationException("Project data missing.");
            
            // In a real scenario, check if ownerId matches project.UserId 
            // For now, we assume the caller has the right to approve.

            request.Status = "Approved";
            request.UpdatedAt = DateTimeOffset.UtcNow;

            // Assign the constructor to the project
            request.Project.ContractorId = request.ConstructorId;

            // Reject other pending requests for the same project
            var otherRequests = await _context.ConstructorProjectRequests
                .Where(r => r.ProjectId == request.ProjectId && r.Id != requestId && r.Status == "Pending")
                .ToListAsync();

            foreach (var other in otherRequests)
            {
                other.Status = "Rejected";
                other.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync();
            return request;
        }

        public async Task<IEnumerable<ConstructorProjectRequest>> GetPendingRequestsForProjectAsync(Guid projectId, Guid ownerId)
        {
            return await _context.ConstructorProjectRequests
                .Include(r => r.Project)
                .Where(r => r.ProjectId == projectId && r.Status == "Pending")
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<ConstructorProjectRequest>> GetConstructorRequestsAsync(Guid constructorId)
        {
            return await _context.ConstructorProjectRequests
                .Include(r => r.Project)
                .Where(r => r.ConstructorId == constructorId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> SetProjectEstimatedDurationAsync(Guid projectId, Guid constructorId, int estimatedDays)
        {
            var project = await _context.Projects
                .Include(p => p.ConstructionPhases)
                .FirstOrDefaultAsync(p => p.Id == projectId && p.ContractorId == constructorId);

            if (project == null) return false;

            // For simplicity, assign all days to the first phase or divide equally.
            // Or if there's only one phase, update it. If none, create one.
            if (!project.ConstructionPhases.Any())
            {
                project.ConstructionPhases.Add(new ConstructionPhase
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    PhaseName = "Main Construction",
                    EstimatedDurationDays = estimatedDays,
                    SequenceOrder = 1,
                    Status = "Not Started"
                });
            }
            else
            {
                // Update the first phase with the estimated days
                var firstPhase = project.ConstructionPhases.OrderBy(p => p.SequenceOrder).First();
                firstPhase.EstimatedDurationDays = estimatedDays;
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
