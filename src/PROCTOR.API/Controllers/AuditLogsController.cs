using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.AuditLogs;
using PROCTOR.Application.Interfaces;
using PROCTOR.Infrastructure.Data;

namespace PROCTOR.API.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize]
[Produces("application/json")]
public class AuditLogsController : ControllerBase
{
    private readonly ProctorDbContext _db;
    private readonly IPermissionChecker _permissionChecker;

    public AuditLogsController(ProctorDbContext db, IPermissionChecker permissionChecker)
    {
        _db = db;
        _permissionChecker = permissionChecker;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? action,
        [FromQuery] string? role,
        [FromQuery] string? entityType,
        [FromQuery] bool? succeeded,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (!await CanViewAsync()) return Forbid();

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 10, 100);
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x =>
                x.UserName.ToLower().Contains(term)
                || x.Action.ToLower().Contains(term)
                || x.EntityType.ToLower().Contains(term)
                || (x.EntityId != null && x.EntityId.ToLower().Contains(term))
                || x.Path.ToLower().Contains(term)
                || (x.IpAddress != null && x.IpAddress.Contains(term)));
        }
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(x => x.Action == action);
        if (!string.IsNullOrWhiteSpace(role)) query = query.Where(x => x.UserRole == role);
        if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(x => x.EntityType == entityType);
        if (succeeded.HasValue) query = query.Where(x => x.Succeeded == succeeded.Value);
        if (from.HasValue) query = query.Where(x => x.CreatedAt >= from.Value.ToUniversalTime());
        if (to.HasValue) query = query.Where(x => x.CreatedAt < to.Value.ToUniversalTime().AddDays(1));

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AuditLogDto
            {
                Id = x.Id,
                UserId = x.UserId,
                UserName = x.UserName,
                UserRole = x.UserRole,
                Action = x.Action,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                HttpMethod = x.HttpMethod,
                Path = x.Path,
                QueryString = x.QueryString,
                StatusCode = x.StatusCode,
                Succeeded = x.Succeeded,
                IpAddress = x.IpAddress,
                UserAgent = x.UserAgent,
                DurationMs = x.DurationMs,
                DetailsJson = x.DetailsJson,
                CreatedAt = x.CreatedAt
            }).ToListAsync();

        return Ok(ApiResponse<PagedResult<AuditLogDto>>.SuccessResponse(new PagedResult<AuditLogDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        }));
    }

    [HttpGet("filters")]
    public async Task<IActionResult> GetFilters()
    {
        if (!await CanViewAsync()) return Forbid();
        var result = new AuditLogFiltersDto
        {
            Actions = await _db.AuditLogs.AsNoTracking().Select(x => x.Action).Distinct().OrderBy(x => x).ToListAsync(),
            Roles = await _db.AuditLogs.AsNoTracking().Select(x => x.UserRole).Distinct().OrderBy(x => x).ToListAsync(),
            EntityTypes = await _db.AuditLogs.AsNoTracking().Select(x => x.EntityType).Distinct().OrderBy(x => x).ToListAsync()
        };
        return Ok(ApiResponse<AuditLogFiltersDto>.SuccessResponse(result));
    }

    private Task<bool> CanViewAsync()
    {
        var role = User.FindFirst("role")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;
        return _permissionChecker.HasPermissionAsync(role, "audit-logs", "read");
    }
}
