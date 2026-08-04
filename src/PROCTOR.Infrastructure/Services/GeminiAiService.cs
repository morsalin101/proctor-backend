using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.Ai;
using PROCTOR.Application.Interfaces;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Interfaces;

namespace PROCTOR.Infrastructure.Services;

/// <summary>
/// Google Gemini backed report drafting.
///
/// The API key lives in the system_settings table (written through Settings → AI Integration)
/// so it can be rotated without a redeploy. A GEMINI_API_KEY environment variable / Gemini:ApiKey
/// configuration entry is used as a fallback when the DB value is empty, which keeps secrets out
/// of source control entirely. The key is never returned to a client — only a masked preview.
/// </summary>
public class GeminiAiService : IAiService
{
    private const string ApiKeySettingKey = "ai_api_key";
    private const string ModelSettingKey = "ai_model";
    private const string ProviderSettingKey = "ai_provider";
    private const string DefaultModel = "gemini-2.5-flash-lite";
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IRepository<SystemSetting> _settingRepo;
    private readonly IRepository<Article> _articleRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public GeminiAiService(
        IHttpClientFactory httpClientFactory,
        IRepository<SystemSetting> settingRepo,
        IRepository<Article> articleRepo,
        IUnitOfWork unitOfWork,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _settingRepo = settingRepo;
        _articleRepo = articleRepo;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    // ---------------------------------------------------------------- settings

    private async Task<SystemSetting?> FindSettingAsync(string key) =>
        (await _settingRepo.FindAsync(s => s.Key == key)).FirstOrDefault();

    private async Task<string> GetApiKeyAsync()
    {
        var stored = (await FindSettingAsync(ApiKeySettingKey))?.Value;
        if (!string.IsNullOrWhiteSpace(stored)) return stored.Trim();

        return _configuration["Gemini:ApiKey"]
            ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? string.Empty;
    }

    private async Task<string> GetModelAsync()
    {
        var stored = (await FindSettingAsync(ModelSettingKey))?.Value;
        return string.IsNullOrWhiteSpace(stored) ? DefaultModel : stored.Trim();
    }

    // Shows enough of the key to recognise it without revealing anything usable.
    private static string Mask(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;
        if (key.Length <= 8) return new string('•', key.Length);
        return $"{key[..4]}{new string('•', 8)}{key[^4..]}";
    }

    public async Task<ApiResponse<AiSettingsDto>> GetSettingsAsync()
    {
        var key = await GetApiKeyAsync();
        var dto = new AiSettingsDto
        {
            Provider = (await FindSettingAsync(ProviderSettingKey))?.Value ?? "gemini",
            Model = await GetModelAsync(),
            HasApiKey = !string.IsNullOrWhiteSpace(key),
            MaskedApiKey = Mask(key)
        };
        return ApiResponse<AiSettingsDto>.SuccessResponse(dto);
    }

    public async Task<ApiResponse<AiSettingsDto>> UpdateSettingsAsync(UpdateAiSettingsRequest request)
    {
        async Task SetAsync(string key, string value, string description)
        {
            var setting = await FindSettingAsync(key);
            if (setting is null)
            {
                await _settingRepo.AddAsync(new SystemSetting
                {
                    Id = Guid.NewGuid(),
                    Key = key,
                    Value = value,
                    Category = "ai",
                    Description = description
                });
            }
            else
            {
                setting.Value = value;
                setting.UpdatedAt = DateTime.UtcNow;
                _settingRepo.Update(setting);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Provider))
            await SetAsync(ProviderSettingKey, request.Provider.Trim(), "AI provider used for report generation");

        if (!string.IsNullOrWhiteSpace(request.Model))
            await SetAsync(ModelSettingKey, request.Model.Trim(), "Model used for AI report generation");

        // An empty ApiKey means "leave the stored key alone" — the client only ever sees the
        // masked value, so it cannot echo the real one back on save.
        if (!string.IsNullOrWhiteSpace(request.ApiKey))
            await SetAsync(ApiKeySettingKey, request.ApiKey.Trim(), "API key for the AI provider (write-only)");

        await _unitOfWork.SaveChangesAsync();
        return await GetSettingsAsync();
    }

    // ---------------------------------------------------------------- test

    public async Task<ApiResponse<AiTestResultDto>> TestConnectionAsync(TestAiConnectionRequest request)
    {
        var key = string.IsNullOrWhiteSpace(request.ApiKey) ? await GetApiKeyAsync() : request.ApiKey.Trim();
        var model = string.IsNullOrWhiteSpace(request.Model) ? await GetModelAsync() : request.Model.Trim();

        if (string.IsNullOrWhiteSpace(key))
        {
            return ApiResponse<AiTestResultDto>.SuccessResponse(new AiTestResultDto
            {
                Valid = false,
                Message = "No API key provided. Paste a key before testing.",
                Model = model
            });
        }

        try
        {
            // A model lookup validates the key AND the model name in one cheap call, without
            // burning generation tokens.
            var client = _httpClientFactory.CreateClient("gemini");
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/models/{model}");
            req.Headers.Add("x-goog-api-key", key);

            using var res = await client.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (res.IsSuccessStatusCode)
            {
                return ApiResponse<AiTestResultDto>.SuccessResponse(new AiTestResultDto
                {
                    Valid = true,
                    Message = $"Connection OK — '{model}' is reachable with this key.",
                    Model = model
                });
            }

            return ApiResponse<AiTestResultDto>.SuccessResponse(new AiTestResultDto
            {
                Valid = false,
                Message = $"{(int)res.StatusCode} {res.StatusCode}: {ExtractApiError(body)}",
                Model = model
            });
        }
        catch (Exception ex)
        {
            return ApiResponse<AiTestResultDto>.SuccessResponse(new AiTestResultDto
            {
                Valid = false,
                Message = $"Could not reach the Gemini API: {ex.Message}",
                Model = model
            });
        }
    }

    // ---------------------------------------------------------------- generation

    public async Task<ApiResponse<GeneratedReportDto>> GenerateCaseReportAsync(Guid caseId, GenerateReportRequest request)
    {
        var c = await _unitOfWork.Cases.GetByIdWithDetailsAsync(caseId);
        if (c is null)
            return ApiResponse<GeneratedReportDto>.FailResponse("Case not found.");

        var key = await GetApiKeyAsync();
        if (string.IsNullOrWhiteSpace(key))
            return ApiResponse<GeneratedReportDto>.FailResponse("No AI API key is configured. Add one under Settings → AI Integration.");

        var model = await GetModelAsync();
        var isBangla = !string.Equals(request.Language, "english", StringComparison.OrdinalIgnoreCase);
        var articles = (await _articleRepo.FindAsync(a => a.IsActive)).OrderBy(a => a.Order).ToList();
        var prompt = BuildPrompt(c, articles, isBangla, request.Instructions);

        try
        {
            var client = _httpClientFactory.CreateClient("gemini");

            // On the 2.5 "flash" models, reasoning tokens are drawn from the same output budget as
            // the answer — a long case can burn the whole budget thinking and return nothing. The
            // report is a formatting task, not a reasoning one, so thinking is switched off there.
            // Pro cannot disable thinking (minimum budget 128), so it keeps the default.
            var disableThinking = model.Contains("flash", StringComparison.OrdinalIgnoreCase);
            object generationConfig = disableThinking
                ? new { temperature = 0.3, maxOutputTokens = 65536, thinkingConfig = new { thinkingBudget = 0 } }
                : new { temperature = 0.3, maxOutputTokens = 65536 };

            var payload = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } },
                generationConfig
            };

            var json = JsonSerializer.Serialize(payload);

            // The free tier returns 503 UNAVAILABLE / 429 whenever a model is busy, which happens
            // often enough on flash-lite that a single attempt would look like a broken feature.
            HttpStatusCode status = default;
            var body = string.Empty;
            for (var attempt = 0; attempt < 3; attempt++)
            {
                if (attempt > 0) await Task.Delay(TimeSpan.FromSeconds(2 * attempt));

                using var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/models/{model}:generateContent")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                req.Headers.Add("x-goog-api-key", key);

                using var res = await client.SendAsync(req);
                status = res.StatusCode;
                body = await res.Content.ReadAsStringAsync();

                if (res.IsSuccessStatusCode) break;
                if (status != HttpStatusCode.ServiceUnavailable && status != HttpStatusCode.TooManyRequests) break;
            }

            if (status != HttpStatusCode.OK)
            {
                return ApiResponse<GeneratedReportDto>.FailResponse(
                    status is HttpStatusCode.ServiceUnavailable or HttpStatusCode.TooManyRequests
                        ? $"The '{model}' model is busy right now. Wait a moment and try again, or switch models under Settings → AI Integration."
                        : $"Gemini returned {(int)status}: {ExtractApiError(body)}");
            }

            var html = ExtractText(body);
            if (string.IsNullOrWhiteSpace(html))
            {
                var reason = ExtractFinishReason(body);
                return ApiResponse<GeneratedReportDto>.FailResponse(reason switch
                {
                    "MAX_TOKENS" => "The model ran out of output budget before finishing. Try a shorter case description or a different model.",
                    "SAFETY" or "PROHIBITED_CONTENT" => "The model declined to draft this report because the case content tripped its safety filters. Write the report manually.",
                    _ => "The model returned an empty response. Try again or pick a different model."
                });
            }

            return ApiResponse<GeneratedReportDto>.SuccessResponse(new GeneratedReportDto
            {
                Html = CleanHtml(html),
                Model = model,
                Language = isBangla ? "bangla" : "english"
            }, "Report drafted.");
        }
        catch (Exception ex)
        {
            return ApiResponse<GeneratedReportDto>.FailResponse($"Could not reach the Gemini API: {ex.Message}");
        }
    }

    // ---------------------------------------------------------------- helpers

    private static string ExtractApiError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var err) &&
                err.TryGetProperty("message", out var msg))
                return msg.GetString() ?? body;
        }
        catch { /* not JSON — fall through */ }
        return body.Length > 300 ? body[..300] : body;
    }

    private static string ExtractText(string body)
    {
        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var candidate in candidates.EnumerateArray())
        {
            if (!candidate.TryGetProperty("content", out var content) ||
                !content.TryGetProperty("parts", out var parts)) continue;

            foreach (var part in parts.EnumerateArray())
                if (part.TryGetProperty("text", out var text))
                    sb.Append(text.GetString());

            break; // first candidate only
        }
        return sb.ToString();
    }

    private static string ExtractFinishReason(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0 &&
                candidates[0].TryGetProperty("finishReason", out var reason))
                return reason.GetString() ?? string.Empty;
        }
        catch { /* not JSON — fall through */ }
        return string.Empty;
    }

    // Models like to wrap output in ```html fences even when told not to.
    private static string CleanHtml(string raw)
    {
        var html = raw.Trim();
        if (html.StartsWith("```"))
        {
            var firstNewline = html.IndexOf('\n');
            if (firstNewline > 0) html = html[(firstNewline + 1)..];
            if (html.EndsWith("```")) html = html[..^3];
        }

        // Despite being told to return a fragment, models sometimes wrap the report in a whole
        // document. Pasting <!DOCTYPE>/<head> into TipTap drops content, so unwrap to the body.
        var bodyMatch = System.Text.RegularExpressions.Regex.Match(
            html, @"<body[^>]*>(.*?)</body>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
        if (bodyMatch.Success)
            html = bodyMatch.Groups[1].Value.Trim();
        else
            html = System.Text.RegularExpressions.Regex.Replace(
                html, @"<!DOCTYPE[^>]*>|</?html[^>]*>|<head[^>]*>.*?</head>|</?body[^>]*>", string.Empty,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline).Trim();

        // Models sometimes "helpfully" swap the logo for a URL they remember from the web. Force
        // every image back to the bundled asset so the report never depends on an outside host.
        html = System.Text.RegularExpressions.Regex.Replace(
            html, """src\s*=\s*(["'])(?!/report_logo\.png)[^"']*\1""", "src=\"/report_logo.png\"",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return html.Trim();
    }

    private static string Line(string label, string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : $"- {label}: {value}\n";

    private static string BuildPrompt(Case c, List<Article> articles, bool isBangla, string? instructions)
    {
        var facts = new StringBuilder();
        facts.Append("CASE RECORD\n");
        facts.Append(Line("Case number", c.CaseNumber));
        facts.Append(Line("Report date (use this as রিপোর্টের তারিখ / Report Date)", DateTime.UtcNow.ToString("yyyy-MM-dd")));
        facts.Append(Line("Case type", c.Type.ToString()));
        facts.Append(Line("Status", c.Status.ToString()));
        facts.Append(Line("Priority", c.Priority.ToString()));
        facts.Append(Line("Subject", c.Subject));
        facts.Append(Line("Category", c.Category?.Name));
        facts.Append(Line("Submitted on", c.CreatedAt.ToString("yyyy-MM-dd")));
        facts.Append(Line("Incident date", c.IncidentDate?.ToString("yyyy-MM-dd")));
        facts.Append(Line("Incident location", c.IncidentLocationDescription));
        facts.Append(Line("Description", c.Description));
        facts.Append(Line("Video evidence link", c.VideoLink));
        facts.Append(Line("Acknowledged by", c.AcknowledgedByName));
        facts.Append(Line("Acknowledgment comment", c.AcknowledgmentComment));
        facts.Append(Line("Verdict recorded", c.Verdict));
        facts.Append(Line("Recommendation recorded", c.Recommendation));

        facts.Append("\nCOMPLAINANTS (name | student id | department | contact | advisor | father's name | father's contact)\n");
        if (c.Complainants.Count > 0)
            foreach (var p in c.Complainants.OrderBy(p => p.Order))
                facts.Append($"- {p.Name} | {p.StudentId} | {p.Department ?? "-"} | {p.Contact ?? "-"} | {p.AdvisorName ?? "-"} | {p.FatherName ?? "-"} | {p.FatherContact ?? "-"}\n");
        else
            facts.Append($"- {c.StudentName} | {c.StudentId} | {c.StudentDepartment ?? "-"} | {c.StudentContact ?? "-"} | {c.StudentAdvisorName ?? "-"} | {c.StudentFatherName ?? "-"} | {c.StudentFatherContact ?? "-"}\n");

        facts.Append("\nACCUSED (name | student id | department | contact | guardian contact)\n");
        if (c.AccusedPersons.Count > 0)
            foreach (var p in c.AccusedPersons.OrderBy(p => p.Order))
                facts.Append($"- {p.Name} | {p.AccusedStudentId} | {p.Department ?? "-"} | {p.Contact ?? "-"} | {p.GuardianContact ?? "-"}\n");
        else if (!string.IsNullOrWhiteSpace(c.AccusedName))
            facts.Append($"- {c.AccusedName} | {c.AccusedId ?? "-"} | {c.AccusedDepartment ?? "-"} | {c.AccusedContact ?? "-"} | {c.AccusedGuardianContact ?? "-"}\n");
        else
            facts.Append("- (none recorded)\n");

        if (c.Hearings.Count > 0)
        {
            facts.Append("\nHEARINGS\n");
            foreach (var h in c.Hearings)
                facts.Append($"- {h.Date} {h.Time} at {h.Location} | status: {h.Status} | participants: {string.Join(", ", h.Participants)} | notes: {h.Notes ?? "-"} | outcome remarks: {h.Remarks ?? "-"}\n");
        }

        if (c.Notes.Count > 0)
        {
            facts.Append("\nCASE NOTES\n");
            foreach (var n in c.Notes.OrderBy(n => n.CreatedAt))
                facts.Append($"- [{n.CreatedAt:yyyy-MM-dd}] {n.Author}: {n.Content}\n");
        }

        if (c.AdditionalInfos.Count > 0)
        {
            facts.Append("\nADDITIONAL INFORMATION\n");
            foreach (var a in c.AdditionalInfos.OrderBy(a => a.CreatedAt))
                facts.Append($"- [{a.CreatedAt:yyyy-MM-dd}] {a.Author} ({a.AuthorRole ?? "-"}): {a.Content}\n");
        }

        if (c.Documents.Count > 0)
        {
            facts.Append("\nATTACHED EVIDENCE\n");
            foreach (var d in c.Documents)
                facts.Append($"- {d.Name} ({d.Type}) uploaded by {d.UploadedBy}\n");
        }

        var officers = c.TimelineEvents
            .Where(e => !string.IsNullOrWhiteSpace(e.User) && e.User != "System")
            .Select(e => e.User).Distinct().ToList();
        if (officers.Count > 0)
            facts.Append($"\nOFFICERS WHO ACTED ON THIS CASE\n- {string.Join("\n- ", officers)}\n");

        if (articles.Count > 0)
        {
            facts.Append("\nUNIVERSITY CODE OF CONDUCT CLAUSES (pick only the ones the facts actually support)\n");
            foreach (var a in articles)
                facts.Append($"- Clause {a.ArticleNo}: {a.Title} — {a.Description}\n");
        }

        var language = isBangla
            ? "Bengali (বাংলা). Use Bengali numerals (১, ২, ৩ …) for section numbers and dates."
            : "English. Use Latin numerals for section numbers and dates.";

        // The case record is entered in a mix of Bangla and English (subjects, departments and
        // notes are usually English). A "write in Bangla" instruction alone leaves those fragments
        // untranslated, and the model tends to silently drop the ones it cannot place — which is
        // why fields went missing from Bangla reports.
        var languageRules = isBangla
            ? """
              - TRANSLATE EVERYTHING INTO BANGLA. The case record below is stored mostly in English;
                it is source data, not text to copy verbatim. Subjects, categories, departments,
                statuses, locations, notes, descriptions and evidence names must all appear in Bangla.
              - Transliterate person names into Bangla script (e.g. "Md. Atikul Islam" → "মোঃ আতিকুল
                ইসলাম", "Rahim Uddin" → "রহিম উদ্দিন"). Do the same for department names
                ("CSE" → "সিএসই", "EEE" → "ইইই").
              - Convert every digit to Bengali numerals — student IDs, phone numbers, dates and times
                included (242-33-145 → ২৪২-৩৩-১৪৫, 01710000123 → ০১৭১০০০০১২৩).
              - The ONLY things that may stay in Latin script are URLs and file names.
              - No English words may remain anywhere in the output.
              """
            : """
              - Write everything in English. If a field in the case record is written in Bangla,
                translate it into English rather than copying the Bangla text through.
              - Transliterate Bangla person and department names into Latin script.
              """;

        var headings = isBangla
            ? """
              ১। অভিযোগকারীদের তথ্য  — table: নাম | আইডি | বিভাগ
              ২। অভিযুক্তদের তথ্য — table: নাম | আইডি | বিভাগ
              ৩। ঘটনার পটভূমিঃ — narrative paragraph(s) of what happened
              ৪। তদন্ত পদ্ধতি এবং অংশগ্রহণকারী: — "তদন্তকারী" table (নাম | পদবী), then a <ul> of the
                 investigation steps (তদন্ত শুরুর তারিখ, জিজ্ঞাসাবাদের তারিখ, জিজ্ঞাসাবাদের ধরন, বক্তব্য),
                 then sub-headings "অভিযোগকারীদের বিবৃতি:" and "অভিযুক্তের বিবৃতি:" each with a <ul> of points,
                 and where the record supports it "অভিযুক্তের অপরাধমূলক রেকর্ড:", "উদ্ধার প্রক্রিয়া:",
                 "পলায়ন ও নিরাপত্তা লঙ্ঘন:".
              ৫। তদন্তের ফলাফলঃ — an <ol> analysis (অপরাধ সংঘটনের প্রক্রিয়া, অপরাধ সংঘটনের কারণ),
                 then "বিশ্ববিদ্যালয়ের কোড অফ কন্ডাক্ট লঙ্ঘন:" with a table (অনুচ্ছেদ নং | অনুচ্ছেদের নাম ও ব্যখ্যা),
                 then "তদন্ত কমিটির চূড়ান্ত মূল্যায়ন:" as a <ul>,
                 then a table (সুপারিশ সমহূ | প্রাসঙ্গিক দলিল/সম্পদ) holding the numbered recommendations.
              ৬। তদন্ত লিখিত রিপোর্টারঃ — table (নাম | পদবী), then "কমিটির সদস্য:" table (নাম | পদবী | স্বাক্ষর)
                 with an empty signature column, then "সংযুক্তি:" list, then a centred closing
                 (ধন্যবাদ / প্রক্টরের নাম ও পদবী / ড্যাফোডিল ইন্টারন্যাশনাল ইউনিভার্সিটি)।
              """
            : """
              1. Complainant Information — table: Name | ID | Department
              2. Accused Information — table: Name | ID | Department
              3. Background of the Incident — narrative paragraph(s)
              4. Investigation Method and Participants — "Investigators" table (Name | Designation), then a
                 <ul> of investigation steps (start date, interrogation date, mode, statements taken), then
                 sub-headings "Statement of the Complainants:" and "Statement of the Accused:" each with a
                 <ul>, and where the record supports it "Prior Criminal Record:", "Recovery Process:",
                 "Escape and Security Breach:".
              5. Investigation Findings — an <ol> analysis (how the offence was committed, why), then
                 "Violation of the University Code of Conduct:" with a table (Clause No | Clause Name and
                 Explanation), then "Final Assessment of the Investigation Committee:" as a <ul>, then a
                 table (Recommendations | Relevant Documents/Evidence) holding the numbered recommendations.
              6. Report Written By — table (Name | Designation), then "Committee Members:" table
                 (Name | Designation | Signature) with an empty signature column, then "Attachments:" list,
                 then a centred closing (Thank you / Proctor's name and designation /
                 Daffodil International University).
              """;

        return $"""
                You are the Assistant Administrative Officer of the Proctor Office at Daffodil International
                University, drafting a formal disciplinary investigation report ("তদন্ত প্রতিবেদন").

                Write the report in {language}

                OUTPUT FORMAT — follow exactly:
                - Return ONLY an HTML fragment. No <html>, <head>, <body>, no markdown code fences, no commentary.
                - Allowed tags only: <h2> <h3> <p> <strong> <em> <u> <ul> <ol> <li> <table> <thead> <tbody> <tr> <th> <td>.
                - Do not add style attributes except `style="text-align: center"` on centred paragraphs/headings.
                - Open with:
                  <p style="text-align: center"><img src="/report_logo.png" alt="Logo"></p>
                  <h2 style="text-align: center"><u>{(isBangla ? "তদন্ত প্রতিবেদন" : "Investigation Report")}</u></h2>
                  <p style="text-align: center">Daffodil International University - Proctor Office</p>
                  then paragraphs for {(isBangla ? "মামলা নম্বর / রিপোর্টের তারিখ / বিষয়" : "Case Number / Report Date / Subject")}.
                - Then these numbered sections as <h3> headings, in this order:
                {headings}

                LANGUAGE RULES — apply to every word you output:
                {languageRules}

                COMPLETENESS — a missing field is a defect:
                - EVERY person and EVERY field present in the case record must appear in the report.
                  If five complainants are listed, the table has five rows. If a father's name, advisor,
                  guardian contact, category, video link or evidence file is recorded, it appears somewhere.
                - Never drop a value because it is written in the other language — translate it and include it.
                - Add extra rows or a short labelled paragraph where a recorded detail does not fit an
                  existing section, rather than discarding it.

                CONTENT RULES — these matter more than style:
                - Use ONLY the facts in the case record below. Never invent names, dates, times, ID numbers,
                  confessions, criminal history, or evidence.
                - Where the record has no information for a required field, emit the placeholder
                  {(isBangla ? "[তথ্য যোগ করুন]" : "[add information]")} so the officer can fill it in. Do not guess.
                - Only cite a code of conduct clause when the recorded facts clearly support it; quote the
                  clause text as given. If none apply, put the placeholder in that table instead.
                - Recommendations must follow from the findings and stay within university disciplinary
                  authority; leave signature cells empty.
                - Keep the tone formal, factual and impersonal, matching an official proctorial report.
                {(string.IsNullOrWhiteSpace(instructions) ? "" : $"\nADDITIONAL INSTRUCTIONS FROM THE OFFICER:\n{instructions}\n")}
                ------------------------------------------------------------------
                {facts}
                ------------------------------------------------------------------
                Now output the HTML fragment only.
                """;
    }
}
