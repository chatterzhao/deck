using Deck.Core.Models;

namespace Deck.Core.Interfaces;

/// <summary>
/// 远程模板服务接口 - 对应 deck-shell 的 remote-templates.sh
/// 处理远程模板仓库的同步、缓存和更新
/// </summary>
public interface IRemoteTemplatesService
{
    /// <summary>
    /// 同步远程模板
    /// </summary>
    Task<SyncResult> SyncTemplatesAsync(bool forceUpdate = false);

    /// <summary>
    /// 获取模板列表
    /// </summary>
    Task<List<TemplateInfo>> GetTemplateListAsync();

    /// <summary>
    /// 检查模板更新
    /// </summary>
    Task<UpdateCheckResult> CheckTemplateUpdatesAsync();

    /// <summary>
    /// 获取模板详细信息
    /// </summary>
    Task<TemplateInfo?> GetTemplateInfoAsync(string templateName);

    /// <summary>
    /// 验证模板完整性
    /// </summary>
    Task<TemplateValidationResult> ValidateTemplateAsync(string templateName);

    /// <summary>
    /// 清理模板缓存
    /// </summary>
    Task<bool> ClearTemplateCacheAsync();
}