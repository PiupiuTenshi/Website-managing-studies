namespace RemoteAssignment.Application.Organization;

public sealed record GradeLevelDto(Guid Id, string Name, int SortOrder);

public sealed record SubjectDto(Guid Id, string Name, string Code);

public sealed record ClassRoomDto(
    Guid Id,
    string Name,
    Guid GradeLevelId,
    string GradeLevelName,
    Guid? ManagerId,
    string? ManagerName);

public sealed record ClassEnrollmentDto(
    Guid Id,
    Guid ClassRoomId,
    Guid StudentId,
    string StudentName,
    string StudentEmail,
    string Status);

public sealed record ParentStudentLinkDto(
    Guid Id,
    Guid ParentId,
    string ParentName,
    Guid StudentId,
    string StudentName,
    string? Relationship,
    string Status);

// --- Request records ---

public sealed record CreateGradeLevelRequest(string Name, int SortOrder);

public sealed record CreateSubjectRequest(string Name, string Code);

public sealed record CreateClassRoomRequest(string Name, Guid GradeLevelId, Guid? ManagerId);

public sealed record UpdateClassRoomRequest(string Name, Guid GradeLevelId, Guid? ManagerId);

public sealed record EnrollStudentRequest(Guid StudentId);

public sealed record LinkParentRequest(Guid ParentId, Guid StudentId, string? Relationship);
