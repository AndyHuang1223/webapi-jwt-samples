using JwtCourseApi.Basic.Models;

namespace JwtCourseApi.Basic.Services;

public interface IDemoUserService
{
    DemoUser? Authenticate(string username, string password);
}
