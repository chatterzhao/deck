namespace Deck.Core.Models;

/// <summary>
/// 三层配置类型枚举
/// </summary>
public enum ThreeLayerConfigurationType
{
    /// <summary>
    /// 已构建镜像 - Images
    /// </summary>
    Images,
    
    /// <summary>
    /// 用户自定义配置 - Custom
    /// </summary>
    Custom,
    
    /// <summary>
    /// 模板库 - Templates
    /// </summary>
    Templates
}

/// <summary>
/// 三层配置选择选项
/// </summary>
public class SelectableThreeLayerConfiguration : ISelectableItem
{
    public string Name { get; set; } = string.Empty;
    public ThreeLayerConfigurationType LayerType { get; set; }
    public string Path { get; set; } = string.Empty;
    public required ConfigurationStatus Status { get; set; }
    public DateTime? LastModified { get; set; }
    public string[] MissingFiles { get; set; } = Array.Empty<string>();
    public ProjectType DetectedProjectType { get; set; } = ProjectType.Unknown;
    public Dictionary<string, string> ExtraProperties { get; set; } = new();

    public string DisplayName => Name;
    
    public string? Description => GetLayerDescription();
    
    public bool IsAvailable => Status.IsComplete;
    
    public string Value => Name;
    
    public string? ExtraInfo => GetExtraInfo();

    private string GetLayerDescription()
    {
        var baseDescription = LayerType switch
        {
            ThreeLayerConfigurationType.Images => "已构建镜像 - 可直接启动",
            ThreeLayerConfigurationType.Custom => "自定义配置 - 可编辑修改",
            ThreeLayerConfigurationType.Templates => "模板库 - 预设配置",
            _ => "未知类型"
        };

        if (DetectedProjectType != ProjectType.Unknown)
        {
            baseDescription += $" ({DetectedProjectType})";
        }

        return baseDescription;
    }

    private string GetExtraInfo()
    {
        var info = new List<string>();
        
        if (LastModified.HasValue)
        {
            info.Add(GetRelativeTime(LastModified.Value));
        }

        if (!Status.IsComplete && MissingFiles.Any())
        {
            info.Add($"缺少 {string.Join(", ", MissingFiles)}");
        }

        return string.Join(" | ", info);
    }

    private static string GetRelativeTime(DateTime dateTime)
    {
        var now = DateTime.Now;
        var diff = now - dateTime;

        return diff.TotalDays switch
        {
            > 30 => $"{(int)(diff.TotalDays / 30)}个月前",
            > 1 => $"{(int)diff.TotalDays}天前",
            _ => diff.TotalHours switch
            {
                > 1 => $"{(int)diff.TotalHours}小时前",
                _ => $"{(int)diff.TotalMinutes}分钟前"
            }
        };
    }
}

/// <summary>
/// 三层选择结果
/// </summary>
public class ThreeLayerSelectionResult
{
    public bool IsSuccess { get; set; }
    public bool IsCancelled { get; set; }
    public SelectableThreeLayerConfiguration? SelectedConfiguration { get; set; }
    public ThreeLayerConfigurationType? SelectedLayerType { get; set; }
    public string? ErrorMessage { get; set; }
    public string? WorkflowChoice { get; set; } // 用于Templates的工作流程选择
}

/// <summary>
/// 三层选择器配置
/// </summary>
public class ThreeLayerSelector
{
    /// <summary>
    /// 提示信息
    /// </summary>
    public string Prompt { get; set; } = "请选择配置：";
    
    /// <summary>
    /// 是否显示项目环境检测信息
    /// </summary>
    public bool ShowProjectDetection { get; set; } = true;
    
    /// <summary>
    /// 检测到的项目类型
    /// </summary>
    public ProjectType DetectedProjectType { get; set; } = ProjectType.Unknown;
    
    /// <summary>
    /// 是否启用环境类型过滤
    /// </summary>
    public bool EnableProjectTypeFilter { get; set; } = true;
    
    /// <summary>
    /// 是否显示配置完整性检查
    /// </summary>
    public bool ShowConfigurationStatus { get; set; } = true;
    
    /// <summary>
    /// Images层配置
    /// </summary>
    public List<SelectableThreeLayerConfiguration> ImagesConfigurations { get; set; } = new();
    
    /// <summary>
    /// Custom层配置
    /// </summary>
    public List<SelectableThreeLayerConfiguration> CustomConfigurations { get; set; } = new();
    
    /// <summary>
    /// Templates层配置
    /// </summary>
    public List<SelectableThreeLayerConfiguration> TemplatesConfigurations { get; set; } = new();
    
    /// <summary>
    /// 是否显示智能提示
    /// </summary>
    public bool ShowSmartHints { get; set; } = true;
    
    /// <summary>
    /// 自定义层级标题
    /// </summary>
    public Dictionary<ThreeLayerConfigurationType, string> LayerTitles { get; set; } = new()
    {
        { ThreeLayerConfigurationType.Images, "【已构建镜像 - Images】" },
        { ThreeLayerConfigurationType.Custom, "【用户自定义配置 - Custom】" },
        { ThreeLayerConfigurationType.Templates, "【模板库 - Templates】" }
    };
}

/// <summary>
/// 键盘导航选项
/// </summary>
public class KeyboardNavigationOptions
{
    /// <summary>
    /// 是否启用键盘导航
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// 向上键
    /// </summary>
    public ConsoleKey UpKey { get; set; } = ConsoleKey.UpArrow;
    
    /// <summary>
    /// 向下键
    /// </summary>
    public ConsoleKey DownKey { get; set; } = ConsoleKey.DownArrow;
    
    /// <summary>
    /// 确认键
    /// </summary>
    public ConsoleKey ConfirmKey { get; set; } = ConsoleKey.Enter;
    
    /// <summary>
    /// 取消键
    /// </summary>
    public ConsoleKey CancelKey { get; set; } = ConsoleKey.Escape;
    
    /// <summary>
    /// 搜索键
    /// </summary>
    public ConsoleKey SearchKey { get; set; } = ConsoleKey.F;
    
    /// <summary>
    /// 帮助键
    /// </summary>
    public ConsoleKey HelpKey { get; set; } = ConsoleKey.F1;
}

/// <summary>
/// 智能提示配置
/// </summary>
public class SmartHintOptions
{
    /// <summary>
    /// 是否启用智能提示
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// 项目类型检测提示
    /// </summary>
    public bool ShowProjectTypeHints { get; set; } = true;
    
    /// <summary>
    /// 配置状态提示
    /// </summary>
    public bool ShowConfigurationStatusHints { get; set; } = true;
    
    /// <summary>
    /// 操作建议提示
    /// </summary>
    public bool ShowActionSuggestions { get; set; } = true;
    
    /// <summary>
    /// 键盘快捷键提示
    /// </summary>
    public bool ShowKeyboardShortcuts { get; set; } = true;
}