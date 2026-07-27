using JwtCourseApi.Advanced.Models;

namespace JwtCourseApi.Advanced.Services;

public sealed class DemoUserService : IDemoUserService
{
    private sealed record DemoUserCredential(DemoUser User, string Password);

    private static readonly IReadOnlyList<DemoUserCredential> Users =
    [
        new(
            new DemoUser("demo-student-001", "student", "課程學生", "Student", "IT"),
            "Student123!"),
        new(
            new DemoUser("demo-admin-001", "admin", "課程管理員", "Admin", "Management"),
            "Admin123!")
    ];

    public DemoUser? Authenticate(string username, string password)
    {
        var credential = Users.FirstOrDefault(item =>
            string.Equals(item.User.Username, username, StringComparison.OrdinalIgnoreCase) &&
            item.Password == password);

        return credential?.User;
    }

    public DemoUser? FindById(string userId)
    {
        return Users
            .Select(item => item.User)
            .FirstOrDefault(user => user.Id == userId);
    }
}
