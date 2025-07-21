namespace Deck.Core.Models;

/// <summary>
/// Deck配置对象，对应.deck/config.yaml文件结构
/// 参考deck-shell的config.yaml设计，保持简单
/// </summary>
public class DeckConfig
{
    /// <summary>
    /// 远程模板仓库配置
    /// </summary>
    public RemoteTemplatesConfig RemoteTemplates { get; set; } = new();
}

/// <summary>
/// 远程模板仓库配置
/// 对应deck-shell中的remote_templates配置
/// </summary>
public class RemoteTemplatesConfig
{
    /// <summary>
    /// 模板仓库URL
    /// </summary>
    public string Repository { get; set; } = "https://github.com/chatterzhao/deck-templates.git";

    /// <summary>
    /// 模板仓库分支
    /// </summary>
    public string Branch { get; set; } = "main";

    /// <summary>
    /// 缓存TTL，默认24小时
    /// </summary>
    public string CacheTtl { get; set; } = "24h";

    /// <summary>
    /// 是否启用自动更新
    /// </summary>
    public bool AutoUpdate { get; set; } = true;
}