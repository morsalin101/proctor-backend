using Microsoft.Extensions.Logging;
using PROCTOR.Application.Interfaces;
using PROCTOR.Domain.Entities;
using PROCTOR.Infrastructure.Data;

namespace PROCTOR.Infrastructure.Services;

public class DemoEmailService : IEmailService
{
    private readonly ProctorDbContext _context;
    private readonly ILogger<DemoEmailService> _logger;

    public DemoEmailService(ProctorDbContext context, ILogger<DemoEmailService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body, Guid? relatedCaseId = null)
    {
        if (string.IsNullOrWhiteSpace(to))
            return;

        _logger.LogInformation(
            "[DemoEmailService] Email pretend-sent. To={To} Subject={Subject} CaseId={CaseId}\nBody:\n{Body}",
            to, subject, relatedCaseId, body);

        var record = new SentEmail
        {
            Id = Guid.NewGuid(),
            To = to,
            Subject = subject,
            Body = body,
            SentAt = DateTime.UtcNow,
            RelatedCaseId = relatedCaseId,
            Provider = "Demo"
        };

        _context.Set<SentEmail>().Add(record);
        await _context.SaveChangesAsync();
    }
}
