using HousePlanner.API.Entities;

namespace HousePlanner.API.Services
{
    public interface IConstructorWorkflowService
    {
        Task<IEnumerable<HousePlanner.API.DTOs.ConstructorProjectDto>> GetConstructorProjectsAsync(Guid constructorId, string userRole);
        Task<Project?> GetProjectEntityAsync(Guid projectId, Guid constructorId, string userRole);
        Task<Project?> GetProjectDetailsAsync(Guid projectId, Guid constructorId, string userRole);
        Task<IEnumerable<ConstructorWorkflowLog>> GetWorkflowLogsAsync(Guid projectId, Guid constructorId, string userRole);
        Task<ConstructorWorkflowLog> CreateWorkflowLogAsync(Guid constructorId, ConstructorWorkflowLog log);
        Task<ConstructorWorkflowLog> UpdateWorkflowLogAsync(Guid constructorId, Guid logId, ConstructorWorkflowLog updatedLog);
        Task<object> GetProjectProgressAsync(Guid projectId, Guid constructorId, string userRole);
        
        // New Assignment Methods
        Task<Project?> SearchProjectByIdAsync(Guid projectId);
        Task<ConstructorProjectRequest> RequestProjectAssignmentAsync(Guid projectId, Guid constructorId);
        Task<ConstructorProjectRequest> ApproveConstructorRequestAsync(Guid requestId, Guid ownerId);
        Task<IEnumerable<ConstructorProjectRequest>> GetPendingRequestsForProjectAsync(Guid projectId, Guid ownerId);
        Task<IEnumerable<ConstructorProjectRequest>> GetConstructorRequestsAsync(Guid constructorId);
        
        // Setup Duration Method
        Task<bool> SetProjectEstimatedDurationAsync(Guid projectId, Guid constructorId, int estimatedDays);
        Task<HousePlanner.API.DTOs.ConstructionPhaseDto?> UpdatePhaseScheduleAsync(Guid projectId, Guid phaseId, Guid constructorId, int plannedDurationDays);
    }
}
