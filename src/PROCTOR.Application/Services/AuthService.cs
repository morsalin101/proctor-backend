using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.Auth;
using PROCTOR.Application.Interfaces;
using PROCTOR.Application.Mapping;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Interfaces;

namespace PROCTOR.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRepository<RefreshToken> _refreshTokenRepo;

    public AuthService(IUnitOfWork unitOfWork, IJwtTokenService jwtTokenService, IRepository<RefreshToken> refreshTokenRepo)
    {
        _unitOfWork = unitOfWork;
        _jwtTokenService = jwtTokenService;
        _refreshTokenRepo = refreshTokenRepo;
    }

    public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
            return ApiResponse<LoginResponse>.FailResponse("Invalid email or password.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return ApiResponse<LoginResponse>.FailResponse("Invalid email or password.");

        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshTokenValue = _jwtTokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        await _refreshTokenRepo.AddAsync(refreshToken);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<LoginResponse>.SuccessResponse(new LoginResponse
        {
            Token = accessToken,
            RefreshToken = refreshTokenValue,
            User = user.ToDto()
        }, "Login successful.");
    }

    public async Task<ApiResponse<TokenResponse>> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var tokens = await _refreshTokenRepo.FindAsync(rt =>
            rt.Token == request.RefreshToken && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow);

        var existingToken = tokens.FirstOrDefault();
        if (existingToken is null)
            return ApiResponse<TokenResponse>.FailResponse("Invalid or expired refresh token.");

        var tokenUser = await _unitOfWork.Users.GetByIdAsync(existingToken.UserId);
        if (tokenUser is null)
            return ApiResponse<TokenResponse>.FailResponse("User not found.");

        // Revoke old token
        existingToken.IsRevoked = true;
        _refreshTokenRepo.Update(existingToken);

        // Generate new tokens
        var newAccessToken = _jwtTokenService.GenerateAccessToken(tokenUser);
        var newRefreshTokenValue = _jwtTokenService.GenerateRefreshToken();

        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = tokenUser.Id,
            Token = newRefreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        await _refreshTokenRepo.AddAsync(newRefreshToken);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<TokenResponse>.SuccessResponse(new TokenResponse
        {
            Token = newAccessToken,
            RefreshToken = newRefreshTokenValue
        }, "Token refreshed successfully.");
    }

    public async Task<ApiResponse<bool>> LogoutAsync(string refreshToken)
    {
        var tokens = await _refreshTokenRepo.FindAsync(rt => rt.Token == refreshToken);
        var token = tokens.FirstOrDefault();

        if (token is null)
            return ApiResponse<bool>.FailResponse("Refresh token not found.");

        token.IsRevoked = true;
        _refreshTokenRepo.Update(token);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<bool>.SuccessResponse(true, "Logged out successfully.");
    }
}
