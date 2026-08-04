namespace PROCTOR.Application.DTOs.Ai;

/// <summary>
/// AI provider configuration as shown in Settings. The API key is never returned in full —
/// only whether one is stored and a masked preview of it.
/// </summary>
public class AiSettingsDto
{
    public string Provider { get; set; } = "gemini";
    public string Model { get; set; } = string.Empty;
    public bool HasApiKey { get; set; }
    public string MaskedApiKey { get; set; } = string.Empty;
}

public class UpdateAiSettingsRequest
{
    public string? Provider { get; set; }
    public string? Model { get; set; }
    /// <summary>Leave null/empty to keep the stored key unchanged.</summary>
    public string? ApiKey { get; set; }
}

public class TestAiConnectionRequest
{
    /// <summary>Leave null/empty to test the key already stored in settings.</summary>
    public string? ApiKey { get; set; }
    public string? Model { get; set; }
}

public class AiTestResultDto
{
    public bool Valid { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Model { get; set; }
}

public class GenerateReportRequest
{
    /// <summary>"bangla" or "english". Defaults to bangla.</summary>
    public string Language { get; set; } = "bangla";
    /// <summary>Optional extra guidance from the officer writing the report.</summary>
    public string? Instructions { get; set; }
}

public class GeneratedReportDto
{
    /// <summary>HTML fragment ready to drop into the TipTap report editor.</summary>
    public string Html { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
}
