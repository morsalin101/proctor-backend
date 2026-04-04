using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.Settings;
using PROCTOR.Application.Interfaces;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Interfaces;

namespace PROCTOR.Application.Services;

public class SystemSettingService : ISystemSettingService
{
    private readonly IRepository<SystemSetting> _settingRepo;
    private readonly IUnitOfWork _unitOfWork;

    public SystemSettingService(IRepository<SystemSetting> settingRepo, IUnitOfWork unitOfWork)
    {
        _settingRepo = settingRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<List<SystemSettingDto>>> GetAllSettingsAsync()
    {
        var settings = await _settingRepo.GetAllAsync();
        return ApiResponse<List<SystemSettingDto>>.SuccessResponse(settings.Select(ToDto).ToList());
    }

    public async Task<ApiResponse<List<SystemSettingDto>>> GetSettingsByCategoryAsync(string category)
    {
        var settings = await _settingRepo.FindAsync(s => s.Category == category);
        return ApiResponse<List<SystemSettingDto>>.SuccessResponse(settings.Select(ToDto).ToList());
    }

    public async Task<ApiResponse<SystemSettingDto>> GetSettingByKeyAsync(string key)
    {
        var settings = await _settingRepo.FindAsync(s => s.Key == key);
        var setting = settings.FirstOrDefault();
        if (setting is null)
            return ApiResponse<SystemSettingDto>.FailResponse("Setting not found.");

        return ApiResponse<SystemSettingDto>.SuccessResponse(ToDto(setting));
    }

    public async Task<ApiResponse<SystemSettingDto>> UpdateSettingAsync(string key, UpdateSettingRequest request)
    {
        var settings = await _settingRepo.FindAsync(s => s.Key == key);
        var setting = settings.FirstOrDefault();
        if (setting is null)
            return ApiResponse<SystemSettingDto>.FailResponse("Setting not found.");

        setting.Value = request.Value;
        setting.UpdatedAt = DateTime.UtcNow;
        _settingRepo.Update(setting);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<SystemSettingDto>.SuccessResponse(ToDto(setting), "Setting updated successfully.");
    }

    private static SystemSettingDto ToDto(SystemSetting s) => new()
    {
        Id = s.Id.ToString(),
        Key = s.Key,
        Value = s.Value,
        Category = s.Category,
        Description = s.Description
    };
}
