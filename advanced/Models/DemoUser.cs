namespace JwtCourseApi.Advanced.Models;

public sealed record DemoUser(
    string Id,
    string Username,
    string DisplayName,
    string Role,
    string Department);
