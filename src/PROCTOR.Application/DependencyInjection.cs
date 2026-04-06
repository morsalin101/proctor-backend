using Microsoft.Extensions.DependencyInjection;
using PROCTOR.Application.Interfaces;
using PROCTOR.Application.Services;

namespace PROCTOR.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICaseService, CaseService>();
        services.AddScoped<IHearingService, HearingService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IRolePermissionService, RolePermissionService>();
        services.AddScoped<ISystemSettingService, SystemSettingService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IWorkflowService, WorkflowService>();
        services.AddScoped<IPermissionChecker, PermissionChecker>();

        return services;
    }
}
