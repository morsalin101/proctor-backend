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
}
