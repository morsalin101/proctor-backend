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

        return services;
    }
}
