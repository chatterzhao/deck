using Deck.Core.Models;

namespace Deck.Core.Interfaces;

/// <summary>
/// 三层统一管理服务 - 管理Images/Custom/Templates三层资源的统一界面和操作
/// </summary>
public interface IImagesUnifiedService
{
    /// <summary>
    /// 获取三层统一资源列表（Images + Custom + Templates）
    /// </summary>
    /// <param name="environmentType">环境类型过滤（可选）</param>
    /// <returns>统一资源列表</returns>
    Task<UnifiedResourceList> GetUnifiedResourceListAsync(string? environmentType = null);
    
    /// <summary>
    /// 获取三层资源关联映射关系
    /// </summary>
    /// <returns>资源关联关系映射</returns>
    Task<Dictionary<string, ResourceRelationship>> GetResourceRelationshipsAsync();
    
    /// <summary>
    /// 获取指定类型的资源详细信息
    /// </summary>
    /// <param name="resourceType">资源类型</param>
    /// <param name="resourceName">资源名称</param>
    /// <returns>资源详细信息</returns>
    Task<UnifiedResourceDetail?> GetResourceDetailAsync(UnifiedResourceType resourceType, string resourceName);
    
    /// <summary>
    /// 获取三层清理选项和策略
    /// </summary>
    /// <returns>清理选项列表</returns>
    Task<List<CleaningOption>> GetCleaningOptionsAsync();
    
    /// <summary>
    /// 执行指定的清理操作
    /// </summary>
    /// <param name="cleaningOption">清理选项</param>
    /// <param name="confirmationCallback">用户确认回调</param>
    /// <returns>清理执行结果</returns>
    Task<CleaningResult> ExecuteCleaningAsync(CleaningOption cleaningOption, Func<string, Task<bool>>? confirmationCallback = null);
    
    /// <summary>
    /// 验证资源完整性（检查配置文件是否完整）
    /// </summary>
    /// <param name="resourceType">资源类型</param>
    /// <param name="resourceName">资源名称</param>
    /// <returns>验证结果</returns>
    Task<ResourceValidationResult> ValidateResourceAsync(UnifiedResourceType resourceType, string resourceName);
}