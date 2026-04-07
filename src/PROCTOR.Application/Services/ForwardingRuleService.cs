using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.ForwardingRules;
using PROCTOR.Application.Interfaces;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Interfaces;

namespace PROCTOR.Application.Services;

public class ForwardingRuleService : IForwardingRuleService
{
    private readonly IRepository<ForwardingRule> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ForwardingRuleService(IRepository<ForwardingRule> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<List<ForwardingRuleDto>>> GetAllAsync()
    {
        var rules = await _repository.GetAllAsync();
        var dtos = rules.Select(r => new ForwardingRuleDto
        {
            Id = r.Id.ToString(), FromRole = r.FromRole, ToRole = r.ToRole,
            ResultStatus = r.ResultStatus, IsActive = r.IsActive
        }).ToList();
        return ApiResponse<List<ForwardingRuleDto>>.SuccessResponse(dtos);
    }

    public async Task<ApiResponse<List<ForwardingRuleDto>>> GetRulesForRoleAsync(string fromRole)
    {
        var rules = await _repository.FindAsync(r => r.FromRole == fromRole && r.IsActive);
        var dtos = rules.Select(r => new ForwardingRuleDto
        {
            Id = r.Id.ToString(), FromRole = r.FromRole, ToRole = r.ToRole,
            ResultStatus = r.ResultStatus, IsActive = r.IsActive
        }).ToList();
        return ApiResponse<List<ForwardingRuleDto>>.SuccessResponse(dtos);
    }

    public async Task<ApiResponse<ForwardingRuleDto>> CreateAsync(CreateForwardingRuleRequest request)
    {
        var existing = await _repository.FindAsync(r => r.FromRole == request.FromRole && r.ToRole == request.ToRole);
        if (existing.Any())
            return ApiResponse<ForwardingRuleDto>.FailResponse("Rule already exists for this role combination.");

        var rule = new ForwardingRule
        {
            Id = Guid.NewGuid(), FromRole = request.FromRole, ToRole = request.ToRole,
            ResultStatus = request.ResultStatus
        };
        await _repository.AddAsync(rule);
        await _unitOfWork.SaveChangesAsync();
        return ApiResponse<ForwardingRuleDto>.SuccessResponse(new ForwardingRuleDto
        {
            Id = rule.Id.ToString(), FromRole = rule.FromRole, ToRole = rule.ToRole,
            ResultStatus = rule.ResultStatus, IsActive = rule.IsActive
        }, "Forwarding rule created.");
    }

    public async Task<ApiResponse<ForwardingRuleDto>> UpdateAsync(Guid id, UpdateForwardingRuleRequest request)
    {
        var rule = await _repository.GetByIdAsync(id);
        if (rule is null) return ApiResponse<ForwardingRuleDto>.FailResponse("Rule not found.");
        if (request.ResultStatus is not null) rule.ResultStatus = request.ResultStatus;
        if (request.IsActive.HasValue) rule.IsActive = request.IsActive.Value;
        rule.UpdatedAt = DateTime.UtcNow;
        _repository.Update(rule);
        await _unitOfWork.SaveChangesAsync();
        return ApiResponse<ForwardingRuleDto>.SuccessResponse(new ForwardingRuleDto
        {
            Id = rule.Id.ToString(), FromRole = rule.FromRole, ToRole = rule.ToRole,
            ResultStatus = rule.ResultStatus, IsActive = rule.IsActive
        }, "Rule updated.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
    {
        var rule = await _repository.GetByIdAsync(id);
        if (rule is null) return ApiResponse<bool>.FailResponse("Rule not found.");
        _repository.Remove(rule);
        await _unitOfWork.SaveChangesAsync();
        return ApiResponse<bool>.SuccessResponse(true, "Rule deleted.");
    }
}
