namespace PROCTOR.Application.DTOs.Ranks;

public class RankDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; }
}

public class CreateRankRequest
{
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
}

public class UpdateRankRequest
{
    public string? Name { get; set; }
    public int? Order { get; set; }
    public bool? IsActive { get; set; }
}
