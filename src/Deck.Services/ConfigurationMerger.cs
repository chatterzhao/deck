using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Deck.Core.Interfaces;

namespace Deck.Services;

/// <summary>
/// 配置合并服务实现
/// 实现基础配置与覆盖配置的合并功能
/// </summary>
public class ConfigurationMerger : IConfigurationMerger
{
    /// <inheritdoc />
    public T Merge<T>(T baseConfig, T overrideConfig) where T : class
    {
        if (baseConfig == null)
            throw new ArgumentNullException(nameof(baseConfig));
        
        if (overrideConfig == null)
            return baseConfig;

        // 对于 DeckConfig 类型，我们使用特定的合并逻辑
        if (typeof(T) == typeof(Core.Models.DeckConfig))
        {
            return (T)(object)MergeDeckConfig(
                (Core.Models.DeckConfig)(object)baseConfig,
                (Core.Models.DeckConfig)(object)overrideConfig);
        }

        return baseConfig;
    }

    private Core.Models.DeckConfig MergeDeckConfig(
        Core.Models.DeckConfig baseConfig, 
        Core.Models.DeckConfig overrideConfig)
    {
        var merged = new Core.Models.DeckConfig
        {
            RemoteTemplates = new Core.Models.RemoteTemplatesConfig()
        };

        // 合并 RemoteTemplates 配置
        // Repository - 只有当override配置中明确设置了值（非null）时才使用它
        if (overrideConfig.RemoteTemplates != null && overrideConfig.RemoteTemplates.Repository != null)
        {
            merged.RemoteTemplates.Repository = overrideConfig.RemoteTemplates.Repository;
        }
        else if (baseConfig.RemoteTemplates != null && baseConfig.RemoteTemplates.Repository != null)
        {
            merged.RemoteTemplates.Repository = baseConfig.RemoteTemplates.Repository;
        }
        else
        {
            merged.RemoteTemplates.Repository = "https://gitee.com/zhaoquan/deck.git";
        }

        // Branch
        if (overrideConfig.RemoteTemplates != null && overrideConfig.RemoteTemplates.Branch != null)
        {
            merged.RemoteTemplates.Branch = overrideConfig.RemoteTemplates.Branch;
        }
        else if (baseConfig.RemoteTemplates != null && baseConfig.RemoteTemplates.Branch != null)
        {
            merged.RemoteTemplates.Branch = baseConfig.RemoteTemplates.Branch;
        }
        else
        {
            merged.RemoteTemplates.Branch = "main";
        }

        // CacheTtl
        if (overrideConfig.RemoteTemplates != null && overrideConfig.RemoteTemplates.CacheTtl != null)
        {
            merged.RemoteTemplates.CacheTtl = overrideConfig.RemoteTemplates.CacheTtl;
        }
        else if (baseConfig.RemoteTemplates != null && baseConfig.RemoteTemplates.CacheTtl != null)
        {
            merged.RemoteTemplates.CacheTtl = baseConfig.RemoteTemplates.CacheTtl;
        }
        else
        {
            merged.RemoteTemplates.CacheTtl = "24h";
        }

        // AutoUpdate
        // 对于布尔值，我们总是使用 overrideConfig 的值（如果已设置）
        if (overrideConfig.RemoteTemplates != null)
        {
            merged.RemoteTemplates.AutoUpdate = overrideConfig.RemoteTemplates.AutoUpdate;
        }
        else if (baseConfig.RemoteTemplates != null)
        {
            merged.RemoteTemplates.AutoUpdate = baseConfig.RemoteTemplates.AutoUpdate;
        }
        else
        {
            merged.RemoteTemplates.AutoUpdate = true;
        }

        return merged;
    }
}