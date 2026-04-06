namespace PROCTOR.Application.DTOs.Documents;

public class DocumentDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string UploadedBy { get; set; } = string.Empty;
    public string? UploadedByRole { get; set; }
    public string UploadedDate { get; set; } = string.Empty;
}
