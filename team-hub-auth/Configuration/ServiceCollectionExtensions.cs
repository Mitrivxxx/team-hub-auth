using System.Text;
using Asp.Versioning;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using team_hub_auth.Data;
using TeamHub.Redis;
using team_hub_auth.Services;
using team_hub_auth.Services.Password;
using team_hub_auth.Services.LoginAttempts;
using team_hub_auth.Services.Sessions;
using team_hub_auth.Services.Tokens;
using team_hub_auth.Services.Users;
using TeamHub.BlobStorage;
using team_hub_auth.Validators;
using TeamHub.Observability;

namespace team_hub_auth.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiInfrastructure(this IServiceCollection services)
    {
        services.AddAuthorization();
        services.AddControllers();
        services.AddTeamHubProblemDetails();
        services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddMvc()
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'V";
                options.SubstituteApiVersionInUrl = true;
            });
        services.AddEndpointsApiExplorer();
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });
        });
        return services;
    }

    public static IServiceCollection AddValidation(this IServiceCollection services)
    {
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
        return services;
    }

    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AuthDbContext>(o =>
            o.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        return services;
    }

    public static IServiceCollection AddJwtConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((options, jwtOptionsAccessor) =>
            {
                var jwtOptions = jwtOptionsAccessor.Value;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key))
                };
            });

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserQueryService, UserQueryService>();
        services.AddScoped<IUserResponseMapper, UserResponseMapper>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IMeProfileService, MeProfileService>();
        services.AddScoped<IUserAvatarService, UserAvatarService>();
        services.AddTeamHubExceptionMapper<RedisUnavailableExceptionMapper>();
        services.AddTeamHubExceptionMapper<AuthExceptionMapper>();
        services.AddGrpc();
        return services;
    }

    public static IServiceCollection AddAuthBlobStorage(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment = null)
    {
        var section = configuration.GetSection(BlobStorageOptions.SectionName);
        var connectionString = configuration.GetConnectionString("blobs")
            ?? section[nameof(BlobStorageOptions.ConnectionString)];

        if (environment?.IsProduction() == true && string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "BlobStorage:ConnectionString is required in Production (Azure Blob or equivalent).");
        }

        services.AddTeamHubBlobStorage(configuration);
        return services;
    }

    public static IServiceCollection AddRedisSessionStore(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTeamHubRedis(configuration);
        services.AddSingleton<ISessionStore, RedisSessionStore>();
        services.AddSingleton<ILoginAttemptLimiter, RedisLoginAttemptLimiter>();

        return services;
    }

    public static IServiceCollection AddAuthHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "postgres")
            .AddCheck<RedisHealthCheck>("redis");

        return services;
    }
}
