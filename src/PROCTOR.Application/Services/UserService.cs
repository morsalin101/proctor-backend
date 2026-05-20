using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.Users;
using PROCTOR.Application.Interfaces;
using PROCTOR.Application.Mapping;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Enums;
using PROCTOR.Domain.Interfaces;

namespace PROCTOR.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IForwardingRuleService _forwardingRuleService;

    public UserService(IUnitOfWork unitOfWork, IForwardingRuleService forwardingRuleService)
    {
        _unitOfWork = unitOfWork;
        _forwardingRuleService = forwardingRuleService;
    }

    public async Task<ApiResponse<List<UserDto>>> GetAllUsersAsync()
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        var dtos = users.Select(u => u.ToDto()).ToList();
        return ApiResponse<List<UserDto>>.SuccessResponse(dtos);
    }

    public async Task<ApiResponse<UserDto>> GetUserByIdAsync(Guid id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user is null)
            return ApiResponse<UserDto>.FailResponse("User not found.");

        return ApiResponse<UserDto>.SuccessResponse(user.ToDto());
    }

    public async Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserRequest request)
    {
        var existingUser = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        if (existingUser is not null)
            return ApiResponse<UserDto>.FailResponse("A user with this email already exists.");

        var role = MappingExtensions.ParseEnum<UserRole>(request.Role);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = role,
            Gender = !string.IsNullOrWhiteSpace(request.Gender)
                ? MappingExtensions.ParseEnum<Gender>(request.Gender)
                : Gender.Unspecified,
            IsActive = true,
            RankName = request.Rank
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<UserDto>.SuccessResponse(user.ToDto(), "User created successfully.");
    }

    public async Task<ApiResponse<UserDto>> UpdateUserAsync(Guid id, UpdateUserRequest request)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user is null)
            return ApiResponse<UserDto>.FailResponse("User not found.");

        if (request.Name is not null)
            user.Name = request.Name;

        if (request.Email is not null)
            user.Email = request.Email;

        if (request.Password is not null)
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        if (request.Role is not null)
            user.Role = MappingExtensions.ParseEnum<UserRole>(request.Role);

        if (request.Gender is not null)
            user.Gender = MappingExtensions.ParseEnum<Gender>(request.Gender);

        if (request.IsActive.HasValue)
            user.IsActive = request.IsActive.Value;

        if (request.Rank is not null)
            user.RankName = request.Rank;

        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<UserDto>.SuccessResponse(user.ToDto(), "User updated successfully.");
    }

    public async Task<ApiResponse<bool>> DeleteUserAsync(Guid id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user is null)
            return ApiResponse<bool>.FailResponse("User not found.");

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<bool>.SuccessResponse(true, "User deleted successfully.");
    }

    public async Task<ApiResponse<List<UserDto>>> GetUsersByRoleAsync(string role)
    {
        var roleEnum = MappingExtensions.ParseEnum<UserRole>(role);
        var users = await _unitOfWork.Users.FindAsync(u => u.Role == roleEnum && u.IsActive);
        var dtos = users.Select(u => u.ToDto()).ToList();
        return ApiResponse<List<UserDto>>.SuccessResponse(dtos);
    }

    public async Task<ApiResponse<List<UserDto>>> GetForwardableUsersAsync(string fromRole)
    {
        // The active forwarding rules ARE the forward permissions: whichever roles
        // `fromRole` may forward to determine whose members appear in the dropdown.
        // GetRulesForRoleAsync already excludes the internal __close__/__hearing__ rules.
        var rulesResponse = await _forwardingRuleService.GetRulesForRoleAsync(fromRole);
        var targetRoles = (rulesResponse.Data ?? new())
            .Where(r => r.IsActive)
            .Select(r => r.ToRole)
            .Distinct()
            .ToList();

        if (targetRoles.Count == 0)
            return ApiResponse<List<UserDto>>.SuccessResponse(new List<UserDto>());

        var roleEnums = new List<UserRole>();
        foreach (var role in targetRoles)
        {
            if (Enum.TryParse<UserRole>(role.Replace("-", ""), true, out var parsed))
                roleEnums.Add(parsed);
        }

        if (roleEnums.Count == 0)
            return ApiResponse<List<UserDto>>.SuccessResponse(new List<UserDto>());

        var users = await _unitOfWork.Users.FindAsync(u => roleEnums.Contains(u.Role) && u.IsActive);
        var dtos = users
            .Select(u => u.ToDto())
            .OrderBy(u => u.Role)
            .ThenBy(u => u.Name)
            .ToList();
        return ApiResponse<List<UserDto>>.SuccessResponse(dtos);
    }
}
