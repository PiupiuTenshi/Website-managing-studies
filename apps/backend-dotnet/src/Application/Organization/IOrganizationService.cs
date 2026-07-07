namespace RemoteAssignment.Application.Organization;

/// <summary>
/// Service interface for organization management: grade levels, subjects, classrooms,
/// class enrollments and parent-student links.
/// </summary>
public interface IOrganizationService
{
    // --- Grade Levels ---
    Task<IReadOnlyList<GradeLevelDto>> ListGradeLevelsAsync(CancellationToken ct);
    Task<GradeLevelDto?> CreateGradeLevelAsync(CreateGradeLevelRequest request, CancellationToken ct);

    // --- Subjects ---
    Task<IReadOnlyList<SubjectDto>> ListSubjectsAsync(CancellationToken ct);
    Task<SubjectDto?> CreateSubjectAsync(CreateSubjectRequest request, CancellationToken ct);

    // --- Class Rooms ---
    Task<IReadOnlyList<ClassRoomDto>> ListClassRoomsAsync(Guid? gradeLevelId, Guid? managerId, CancellationToken ct);
    Task<ClassRoomDto?> GetClassRoomAsync(Guid id, CancellationToken ct);
    Task<ClassRoomDto?> CreateClassRoomAsync(CreateClassRoomRequest request, CancellationToken ct);
    Task<ClassRoomDto?> UpdateClassRoomAsync(Guid id, UpdateClassRoomRequest request, CancellationToken ct);
    Task<bool> DeleteClassRoomAsync(Guid id, CancellationToken ct);

    // --- Class Enrollments ---
    Task<IReadOnlyList<ClassEnrollmentDto>> ListEnrollmentsAsync(Guid classRoomId, CancellationToken ct);
    Task<ClassEnrollmentDto?> EnrollStudentAsync(Guid classRoomId, EnrollStudentRequest request, CancellationToken ct);
    Task<bool> RemoveEnrollmentAsync(Guid classRoomId, Guid studentId, CancellationToken ct);

    // --- Parent-Student Links ---
    Task<IReadOnlyList<ParentStudentLinkDto>> ListLinksByParentAsync(Guid parentId, CancellationToken ct);
    Task<IReadOnlyList<ParentStudentLinkDto>> ListLinksByStudentAsync(Guid studentId, CancellationToken ct);
    Task<ParentStudentLinkDto?> LinkParentAsync(LinkParentRequest request, CancellationToken ct);
    Task<bool> UnlinkParentAsync(Guid linkId, CancellationToken ct);
}
