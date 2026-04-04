using PROCTOR.Domain.Entities;

namespace PROCTOR.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}
