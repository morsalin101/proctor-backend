namespace PROCTOR.Application.DTOs.Cases;

public class UpdateCaseRequest
{
    public string? Priority { get; set; }
    public string? Description { get; set; }
    public string? AssignedToId { get; set; }
}
