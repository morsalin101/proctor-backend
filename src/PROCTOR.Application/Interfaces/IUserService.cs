using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.Users;

namespace PROCTOR.Application.Interfaces;

public interface IUserService
{
    Task<ApiResponse<List<UserDto>>> GetAllUsersAsync();
    Task<ApiResponse<UserDto>> GetUserByIdAsync(Guid id);
    Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserRequest request);
    Task<ApiResponse<UserDto>> UpdateUserAsync(Guid id, UpdateUserRequest request);
    Task<ApiResponse<bool>> DeleteUserAsync(Guid id);
    Task<ApiResponse<List<UserDto>>> GetUsersByRoleAsync(string role);

    /// <summary>
    /// Returns all active users that the given role is permitted to forward a case to,
    /// based on the active forwarding rules (fromRole -> toRole). Each user carries its
    /// own role so the UI can group the unified Forward dropdown by role.
    /// </summary>
    Task<ApiResponse<List<UserDto>>> GetForwardableUsersAsync(string fromRole);
}
