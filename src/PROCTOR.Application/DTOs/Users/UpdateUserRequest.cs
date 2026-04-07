namespace PROCTOR.Application.DTOs.Users;

public class UpdateUserRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
    public string? Rank { get; set; }
}
