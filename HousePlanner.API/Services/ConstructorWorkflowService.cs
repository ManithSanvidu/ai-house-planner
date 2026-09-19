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

        public async Task<IEnumerable<Project>> GetConstructorProjectsAsync(Guid constructorId, string userRole)
        {
            var query = _context.Projects.Include(p => p.ConstructionPhases).AsQueryable();

            if (!string.Equals(userRole, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.ContractorId == constructorId);
            }

            return await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        }

        public async Task<Project?> GetProjectDetailsAsync(Guid projectId, Guid constructorId, string userRole)
        {
            var projects = await GetConstructorProjectsAsync(constructorId, userRole);
            var project = projects.FirstOrDefault(p => p.Id == projectId);
            
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
    }
}
