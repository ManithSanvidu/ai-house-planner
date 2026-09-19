using HousePlanner.API.Entities;

namespace HousePlanner.API.Services
{
    public interface IConstructorWorkflowService
    {
        Task<IEnumerable<Project>> GetConstructorProjectsAsync(Guid constructorId, string userRole);
        Task<Project?> GetProjectDetailsAsync(Guid projectId, Guid constructorId, string userRole);
        Task<IEnumerable<ConstructorWorkflowLog>> GetWorkflowLogsAsync(Guid projectId, Guid constructorId, string userRole);
        Task<ConstructorWorkflowLog> CreateWorkflowLogAsync(Guid constructorId, ConstructorWorkflowLog log);
        Task<ConstructorWorkflowLog> UpdateWorkflowLogAsync(Guid constructorId, Guid logId, ConstructorWorkflowLog updatedLog);
        Task<object> GetProjectProgressAsync(Guid projectId, Guid constructorId, string userRole);
    }
}
