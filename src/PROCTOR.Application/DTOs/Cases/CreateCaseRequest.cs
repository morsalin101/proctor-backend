namespace PROCTOR.Application.DTOs.Cases;

public class CreateCaseRequest
{
    public string StudentName { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
