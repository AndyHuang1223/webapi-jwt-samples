using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using JwtCourseApi.Basic.Filters;
using JwtCourseApi.Basic.Options;
using JwtCourseApi.Basic.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Add JWT configuration options to the DI container
// option1: Add JWT configuration options
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

// option2: Add JWT configuration options with validation
// builder.Services.AddOptions<JwtOptions>()
//     .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
//     .ValidateDataAnnotations()
//     .Validate(options => !string.IsNullOrWhiteSpace(options.SigningKey),
//         "Jwt:SigningKey must be provided through User Secrets or another secure configuration source.")
//     .ValidateOnStart();


builder.Services.AddSingleton<IDemoUserService, DemoUserService>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? throw new InvalidOperationException("The Jwt configuration section is missing.");

        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,

            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),

            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("ItDepartmentOnly", policy => policy.RequireClaim("department", "IT"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "JWT Course API",
        Version = "v1",
        Description = "ASP.NET Core .NET 10 JWT authentication and authorization sample."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "輸入 JWT，不需要手動加上 Bearer 前綴。"
    });
    options.OperationFilter<AuthorizeOperationFilter>();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
