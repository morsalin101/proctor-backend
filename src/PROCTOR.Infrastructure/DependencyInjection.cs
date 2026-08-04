using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PROCTOR.Domain.Interfaces;
using PROCTOR.Infrastructure.Data;
using PROCTOR.Infrastructure.Repositories;
using PROCTOR.Infrastructure.Services;
using PROCTOR.Application.Interfaces;

namespace PROCTOR.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ProctorDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IEmailService, DemoEmailService>();

        // Report drafting via Google Gemini. Generation can take a while on long cases, so the
        // client gets a generous timeout rather than the 100s default.
        services.AddHttpClient("gemini", client => client.Timeout = TimeSpan.FromMinutes(3));
        services.AddScoped<IAiService, GeminiAiService>();

        return services;
    }
}
