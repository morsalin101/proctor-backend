namespace PROCTOR.Application.DTOs.Cases;

public class AdditionalInfoDto
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? AuthorRole { get; set; }
    public string CreatedDate { get; set; } = string.Empty;
}

public class AddAdditionalInfoRequest
{
    public string Content { get; set; } = string.Empty;
}
