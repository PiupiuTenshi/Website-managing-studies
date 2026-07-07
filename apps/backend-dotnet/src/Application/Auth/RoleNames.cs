namespace RemoteAssignment.Application.Auth;

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Student = "Student";
    public const string Parent = "Parent";

    public static readonly string[] All = [Admin, Manager, Student, Parent];
}
