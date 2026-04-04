using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.Settings;

namespace PROCTOR.Application.Interfaces;

public interface ISystemSettingService
{
    Task<ApiResponse<List<SystemSettingDto>>> GetAllSettingsAsync();
    Task<ApiResponse<List<SystemSettingDto>>> GetSettingsByCategoryAsync(string category);
    Task<ApiResponse<SystemSettingDto>> GetSettingByKeyAsync(string key);
    Task<ApiResponse<SystemSettingDto>> UpdateSettingAsync(string key, UpdateSettingRequest request);
}
