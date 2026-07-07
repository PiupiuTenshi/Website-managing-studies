namespace RemoteAssignment.Application.AssignmentAuthoring;

public interface IAssignmentAuthoringService
{
    Task<IReadOnlyList<AssignmentDto>> ListAssignmentsAsync(Guid? subjectId, Guid? createdBy, string? status, CancellationToken ct);
    Task<AssignmentDto?> GetAssignmentAsync(Guid id, CancellationToken ct);
    Task<AssignmentDto?> CreateAssignmentAsync(CreateAssignmentRequest request, Guid createdBy, CancellationToken ct);
    Task<AssignmentDto?> UpdateAssignmentAsync(Guid id, UpdateAssignmentRequest request, CancellationToken ct);
    Task<bool> DeleteAssignmentAsync(Guid id, CancellationToken ct);
    
    Task<AssignmentDto?> PublishAssignmentAsync(Guid id, CancellationToken ct);
    Task<AssignmentDto?> ArchiveAssignmentAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<AssignmentTargetDto>> GetAssignmentTargetsAsync(Guid assignmentId, CancellationToken ct);
    Task<bool> SetAssignmentTargetsAsync(Guid assignmentId, AssignTargetRequest request, CancellationToken ct);
}
