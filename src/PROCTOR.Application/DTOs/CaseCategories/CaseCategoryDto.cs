namespace PROCTOR.Application.DTOs.CaseCategories;

public class CaseCategoryDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsConfidential { get; set; }
    public bool IsActive { get; set; }
    public string AppliesToType { get; set; } = "both";
    public int SortOrder { get; set; }
}

public class CreateCaseCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsConfidential { get; set; }
    public bool IsActive { get; set; } = true;
    public string AppliesToType { get; set; } = "both";
    public int SortOrder { get; set; }
}

public class UpdateCaseCategoryRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsConfidential { get; set; }
    public bool? IsActive { get; set; }
    public string? AppliesToType { get; set; }
    public int? SortOrder { get; set; }
}
