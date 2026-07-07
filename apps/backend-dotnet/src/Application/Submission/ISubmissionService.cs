namespace RemoteAssignment.Application.Submission;

public interface ISubmissionService
{
    // Student APIs
    Task<IReadOnlyList<StudentAssignmentDto>> GetStudentAssignmentsAsync(Guid studentId, CancellationToken ct);
    Task<SubmissionDto?> GetSubmissionByAssignmentAsync(Guid assignmentId, Guid studentId, CancellationToken ct);
    Task<SubmissionDto?> DraftSubmissionAsync(Guid assignmentId, Guid studentId, DraftSubmissionRequest request, CancellationToken ct);
    Task<SubmissionDto?> SubmitAsync(Guid assignmentId, Guid studentId, SubmitAssignmentRequest request, CancellationToken ct);

    // Manager APIs
    Task<IReadOnlyList<ManagerSubmissionDto>> GetSubmissionsForAssignmentAsync(Guid assignmentId, CancellationToken ct);
    Task<SubmissionDto?> GetSubmissionDetailForManagerAsync(Guid submissionId, CancellationToken ct);
    Task<SubmissionDto?> GradeSubmissionAsync(Guid submissionId, GradeSubmissionRequest request, CancellationToken ct);
}
