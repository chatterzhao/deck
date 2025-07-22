namespace Deck.Core.Models;

/// <summary>
/// Start 命令执行结果
/// </summary>
public class StartCommandResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 启动的镜像名称
    /// </summary>
    public string? ImageName { get; set; }

    /// <summary>
    /// 容器名称
    /// </summary>
    public string? ContainerName { get; set; }

    /// <summary>
    /// 开发信息
    /// </summary>
    public DevelopmentInfo? DevelopmentInfo { get; set; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static StartCommandResult Success(string imageName, string containerName, DevelopmentInfo? devInfo = null)
    {
        return new StartCommandResult
        {
            IsSuccess = true,
            ImageName = imageName,
            ContainerName = containerName,
            DevelopmentInfo = devInfo
        };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static StartCommandResult Failure(string errorMessage)
    {
        return new StartCommandResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}

/// <summary>
/// Start命令专用的三层配置选项扩展
/// </summary>
public class StartCommandThreeLayerOptions
{
    /// <summary>
    /// 环境类型
    /// </summary>
    public string EnvType { get; set; } = string.Empty;

    /// <summary>
    /// 是否为自动检测
    /// </summary>
    public bool IsAutoDetected { get; set; }

    /// <summary>
    /// 镜像选项列表
    /// </summary>
    public List<ImageOption> Images { get; set; } = new();

    /// <summary>
    /// 自定义配置选项列表
    /// </summary>
    public List<ConfigOption> Configs { get; set; } = new();

    /// <summary>
    /// 模板选项列表
    /// </summary>
    public List<TemplateOption> Templates { get; set; } = new();

    /// <summary>
    /// 获取所有可用选项
    /// </summary>
    public List<StartCommandSelectableOption> GetAllOptions()
    {
        var options = new List<StartCommandSelectableOption>();
        var optionNumber = 1;

        // 添加镜像选项
        foreach (var image in Images)
        {
            options.Add(new StartCommandSelectableOption
            {
                Number = optionNumber++,
                Type = OptionType.Image,
                Name = image.Name,
                DisplayName = $"{image.Name} ({image.RelativeTime})",
                IsAvailable = image.IsAvailable,
                UnavailableReason = image.UnavailableReason
            });
        }

        // 添加配置选项
        foreach (var config in Configs)
        {
            options.Add(new StartCommandSelectableOption
            {
                Number = optionNumber++,
                Type = OptionType.Config,
                Name = config.Name,
                DisplayName = config.Name,
                IsAvailable = config.IsAvailable,
                UnavailableReason = config.UnavailableReason
            });
        }

        // 添加模板选项
        foreach (var template in Templates)
        {
            options.Add(new StartCommandSelectableOption
            {
                Number = optionNumber++,
                Type = OptionType.Template,
                Name = template.Name,
                DisplayName = template.IsBuiltIn ? $"{template.Name} (内置)" : template.Name,
                IsAvailable = template.IsAvailable,
                UnavailableReason = template.UnavailableReason
            });
        }

        return options;
    }
}

/// <summary>
/// 镜像选项
/// </summary>
public class ImageOption
{
    /// <summary>
    /// 镜像名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 镜像路径
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 相对时间
    /// </summary>
    public string RelativeTime { get; set; } = string.Empty;

    /// <summary>
    /// 是否可用
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// 不可用原因
    /// </summary>
    public string? UnavailableReason { get; set; }
}

/// <summary>
/// 配置选项
/// </summary>
public class ConfigOption
{
    /// <summary>
    /// 配置名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 配置路径
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 是否可用
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// 不可用原因
    /// </summary>
    public string? UnavailableReason { get; set; }
}

/// <summary>
/// 模板选项
/// </summary>
public class TemplateOption
{
    /// <summary>
    /// 模板名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 模板路径
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 是否为内置模板
    /// </summary>
    public bool IsBuiltIn { get; set; }

    /// <summary>
    /// 是否可用
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// 不可用原因
    /// </summary>
    public string? UnavailableReason { get; set; }
}

/// <summary>
/// Start命令专用的可选择选项
/// </summary>
public class StartCommandSelectableOption
{
    /// <summary>
    /// 选项编号
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// 选项类型
    /// </summary>
    public OptionType Type { get; set; }

    /// <summary>
    /// 选项名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 是否可用
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// 不可用原因
    /// </summary>
    public string? UnavailableReason { get; set; }
}

/// <summary>
/// 选项类型
/// </summary>
public enum OptionType
{
    /// <summary>
    /// 镜像
    /// </summary>
    Image,

    /// <summary>
    /// 配置
    /// </summary>
    Config,

    /// <summary>
    /// 模板
    /// </summary>
    Template
}

/// <summary>
/// 模板工作流程类型
/// </summary>
public enum TemplateWorkflowType
{
    /// <summary>
    /// 创建可编辑配置
    /// </summary>
    CreateEditableConfig,

    /// <summary>
    /// 直接构建启动
    /// </summary>
    DirectBuildAndStart
}

/// <summary>
/// 开发信息
/// </summary>
public class DevelopmentInfo
{
    /// <summary>
    /// 开发端口
    /// </summary>
    public int DevPort { get; set; }

    /// <summary>
    /// 调试端口
    /// </summary>
    public int DebugPort { get; set; }

    /// <summary>
    /// Web端口
    /// </summary>
    public int WebPort { get; set; }

    /// <summary>
    /// 开发环境URL
    /// </summary>
    public string DevUrl => $"http://localhost:{DevPort}";

    /// <summary>
    /// Web环境URL
    /// </summary>
    public string WebUrl => $"http://localhost:{WebPort}";
}