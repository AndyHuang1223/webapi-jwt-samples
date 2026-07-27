using JwtCourseApi.Advanced.Models;

namespace JwtCourseApi.Advanced.Services;

public interface IDemoUserService
{
    DemoUser? Authenticate(string username, string password);

    DemoUser? FindById(string userId);
}
