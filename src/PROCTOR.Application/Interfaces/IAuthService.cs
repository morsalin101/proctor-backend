using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.Auth;

namespace PROCTOR.Application.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request);
    Task<ApiResponse<TokenResponse>> RefreshTokenAsync(RefreshTokenRequest request);
    Task<ApiResponse<bool>> LogoutAsync(string refreshToken);
}
