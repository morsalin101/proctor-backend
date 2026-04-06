namespace PROCTOR.Application.Interfaces;

public interface IPermissionChecker
{
    Task<bool> HasPermissionAsync(string userRole, string menuKey, string operation);
}
