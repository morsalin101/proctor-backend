using Microsoft.EntityFrameworkCore;
using PROCTOR.Domain.Entities;
using PROCTOR.Infrastructure.Data;

namespace PROCTOR.Infrastructure.Seeders;

public static class ForwardingRuleSeeder
{
    public static async Task SeedAsync(ProctorDbContext context)
    {
        if (await context.ForwardingRules.AnyAsync()) return;

        var rules = new List<ForwardingRule>
        {
            new() { Id = Guid.NewGuid(), FromRole = "coordinator", ToRole = "proctor", ResultStatus = "assigned" },
            new() { Id = Guid.NewGuid(), FromRole = "female-coordinator", ToRole = "proctor", ResultStatus = "assigned" },
            new() { Id = Guid.NewGuid(), FromRole = "coordinator", ToRole = "sexual-harassment-committee", ResultStatus = "assigned" },
            new() { Id = Guid.NewGuid(), FromRole = "female-coordinator", ToRole = "sexual-harassment-committee", ResultStatus = "assigned" },
            new() { Id = Guid.NewGuid(), FromRole = "proctor", ToRole = "assistant-proctor", ResultStatus = "assigned" },
            new() { Id = Guid.NewGuid(), FromRole = "proctor", ToRole = "deputy-proctor", ResultStatus = "assigned" },
            new() { Id = Guid.NewGuid(), FromRole = "proctor", ToRole = "registrar", ResultStatus = "forwarded-to-registrar" },
            new() { Id = Guid.NewGuid(), FromRole = "assistant-proctor", ToRole = "deputy-proctor", ResultStatus = "assigned" },
            new() { Id = Guid.NewGuid(), FromRole = "deputy-proctor", ToRole = "assistant-proctor", ResultStatus = "assigned" },
            new() { Id = Guid.NewGuid(), FromRole = "deputy-proctor", ToRole = "proctor", ResultStatus = "assigned" },
            new() { Id = Guid.NewGuid(), FromRole = "registrar", ToRole = "proctor", ResultStatus = "assigned" },
            new() { Id = Guid.NewGuid(), FromRole = "registrar", ToRole = "disciplinary-committee", ResultStatus = "forwarded-to-committee" },
            new() { Id = Guid.NewGuid(), FromRole = "sexual-harassment-committee", ToRole = "assistant-proctor", ResultStatus = "assigned" },
            new() { Id = Guid.NewGuid(), FromRole = "sexual-harassment-committee", ToRole = "deputy-proctor", ResultStatus = "assigned" },
            new() { Id = Guid.NewGuid(), FromRole = "sexual-harassment-committee", ToRole = "registrar", ResultStatus = "forwarded-to-registrar" },
            new() { Id = Guid.NewGuid(), FromRole = "disciplinary-committee", ToRole = "proctor", ResultStatus = "assigned" },
        };

        await context.ForwardingRules.AddRangeAsync(rules);
        await context.SaveChangesAsync();
    }
}
