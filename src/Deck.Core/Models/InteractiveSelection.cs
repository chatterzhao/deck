namespace Deck.Core.Models;

/// <summary>
/// 可选择项接口 - 用于统一交互式选择
/// </summary>
public interface ISelectableItem
{
    /// <summary>
    /// 显示名称
    /// </summary>
    string DisplayName { get; }
    
    /// <summary>
    /// 描述信息
    /// </summary>
    string? Description { get; }
    
    /// <summary>
    /// 是否可用
    /// </summary>
    bool IsAvailable { get; }
    
    /// <summary>
    /// 选择值
    /// </summary>
    string Value { get; }
    
    /// <summary>
    /// 额外信息
    /// </summary>
    string? ExtraInfo { get; }
}

/// <summary>
/// 选择选项实现 - 基础的可选择项
/// </summary>
public class SelectableOption : ISelectableItem
{
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsAvailable { get; set; } = true;
    public string Value { get; set; } = string.Empty;
    public string? ExtraInfo { get; set; }
}

/// <summary>
/// 交互式选择器配置
/// </summary>
public class InteractiveSelector<T> where T : ISelectableItem
{
    /// <summary>
    /// 提示信息
    /// </summary>
    public string Prompt { get; set; } = string.Empty;
    
    /// <summary>
    /// 可选择项列表
    /// </summary>
    public List<T> Items { get; set; } = new();
    
    /// <summary>
    /// 是否允许多选
    /// </summary>
    public bool AllowMultiple { get; set; } = false;
    
    /// <summary>
    /// 是否必须选择
    /// </summary>
    public bool Required { get; set; } = true;
    
    /// <summary>
    /// 默认选中项索引
    /// </summary>
    public int DefaultIndex { get; set; } = 0;
    
    /// <summary>
    /// 每页显示数量
    /// </summary>
    public int PageSize { get; set; } = 10;
    
    /// <summary>
    /// 是否显示索引
    /// </summary>
    public bool ShowIndex { get; set; } = true;
    
    /// <summary>
    /// 是否显示描述
    /// </summary>
    public bool ShowDescription { get; set; } = true;
    
    /// <summary>
    /// 搜索占位符
    /// </summary>
    public string? SearchPlaceholder { get; set; }
    
    /// <summary>
    /// 是否启用搜索
    /// </summary>
    public bool EnableSearch { get; set; } = true;
}

/// <summary>
/// 交互式选择结果
/// </summary>
public class SelectionResult<T> where T : ISelectableItem
{
    /// <summary>
    /// 是否取消选择
    /// </summary>
    public bool IsCancelled { get; set; }
    
    /// <summary>
    /// 选中的项
    /// </summary>
    public T? SelectedItem { get; set; }
    
    /// <summary>
    /// 选中的项列表（多选）
    /// </summary>
    public List<T> SelectedItems { get; set; } = new();
    
    /// <summary>
    /// 选中的索引
    /// </summary>
    public int SelectedIndex { get; set; } = -1;
    
    /// <summary>
    /// 选中的索引列表（多选）
    /// </summary>
    public List<int> SelectedIndices { get; set; } = new();
}

/// <summary>
/// 选择器显示模式
/// </summary>
public enum SelectionDisplayMode
{
    /// <summary>
    /// 列表模式
    /// </summary>
    List,
    
    /// <summary>
    /// 表格模式
    /// </summary>
    Table,
    
    /// <summary>
    /// 紧凑模式
    /// </summary>
    Compact
}

/// <summary>
/// 选择器样式配置
/// </summary>
public class SelectionStyle
{
    /// <summary>
    /// 显示模式
    /// </summary>
    public SelectionDisplayMode DisplayMode { get; set; } = SelectionDisplayMode.List;
    
    /// <summary>
    /// 高亮颜色
    /// </summary>
    public ConsoleColor HighlightColor { get; set; } = ConsoleColor.Cyan;
    
    /// <summary>
    /// 选中颜色
    /// </summary>
    public ConsoleColor SelectedColor { get; set; } = ConsoleColor.Green;
    
    /// <summary>
    /// 不可用项颜色
    /// </summary>
    public ConsoleColor DisabledColor { get; set; } = ConsoleColor.DarkGray;
    
    /// <summary>
    /// 边框样式
    /// </summary>
    public BorderStyle BorderStyle { get; set; } = BorderStyle.Simple;
    
    /// <summary>
    /// 是否显示边框
    /// </summary>
    public bool ShowBorder { get; set; } = true;
    
    /// <summary>
    /// 缩进空格数
    /// </summary>
    public int IndentSpaces { get; set; } = 2;
}

/// <summary>
/// 边框样式
/// </summary>
public enum BorderStyle
{
    /// <summary>
    /// 简单
    /// </summary>
    Simple,
    
    /// <summary>
    /// 双线
    /// </summary>
    Double,
    
    /// <summary>
    /// 圆角
    /// </summary>
    Rounded,
    
    /// <summary>
    /// 无边框
    /// </summary>
    None
}

/// <summary>
/// 镜像选择选项 - 基于 ImageInfo 的可选择项
/// </summary>
public class SelectableImage : ISelectableItem
{
    private readonly ImageInfo _imageInfo;
    
    public SelectableImage(ImageInfo imageInfo)
    {
        _imageInfo = imageInfo;
    }
    
    public string DisplayName => $"{_imageInfo.Name}:{_imageInfo.Tag}";
    public string? Description => $"ID: {_imageInfo.Id[..Math.Min(12, _imageInfo.Id.Length)]}";
    public bool IsAvailable => _imageInfo.Exists;
    public string Value => _imageInfo.Id;
    public string? ExtraInfo => $"{FormatSize(_imageInfo.Size)} | {_imageInfo.Created:yyyy-MM-dd}";
    
    public ImageInfo ImageInfo => _imageInfo;
    
    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

/// <summary>
/// 容器选择选项 - 基于容器信息的可选择项
/// </summary>
public class SelectableContainer : ISelectableItem
{
    public string ContainerId { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
    public ContainerStatus Status { get; set; }
    public string ImageName { get; set; } = string.Empty;
    public DateTime? Created { get; set; }
    public Dictionary<string, string> Ports { get; set; } = new();
    
    public string DisplayName => string.IsNullOrEmpty(ContainerName) ? 
        ContainerId[..Math.Min(12, ContainerId.Length)] : ContainerName;
    
    public string? Description => $"{ImageName} | {Status}";
    
    public bool IsAvailable => Status != ContainerStatus.Error;
    
    public string Value => ContainerId;
    
    public string? ExtraInfo => 
        $"{(Created?.ToString("yyyy-MM-dd HH:mm") ?? "Unknown")} | {GetPortsInfo()}";
    
    private string GetPortsInfo()
    {
        if (!Ports.Any()) return "No ports";
        return string.Join(", ", Ports.Select(p => $"{p.Key}→{p.Value}"));
    }
}

/// <summary>
/// 项目类型选择选项
/// </summary>
public class SelectableProjectType : ISelectableItem
{
    public ProjectType ProjectType { get; set; }
    public string[] SupportedExtensions { get; set; } = Array.Empty<string>();
    public string DefaultTemplate { get; set; } = string.Empty;
    
    public string DisplayName => ProjectType.ToString();
    
    public string? Description => GetProjectTypeDescription();
    
    public bool IsAvailable { get; set; } = true;
    
    public string Value => ProjectType.ToString();
    
    public string? ExtraInfo => $"Extensions: {string.Join(", ", SupportedExtensions)}";
    
    private string GetProjectTypeDescription()
    {
        return ProjectType switch
        {
            ProjectType.Tauri => "Cross-platform desktop application with Rust + Web",
            ProjectType.Flutter => "Cross-platform mobile and desktop with Dart",
            ProjectType.Avalonia => ".NET cross-platform desktop application",
            ProjectType.DotNet => ".NET console, web, or library application",
            ProjectType.Python => "Python application or script",
            ProjectType.Node => "Node.js application or service",
            ProjectType.Unknown => "Unknown or unsupported project type",
            _ => "Unknown project type"
        };
    }
}

/// <summary>
/// 命令选择选项
/// </summary>
public class SelectableCommand : ISelectableItem
{
    public string Command { get; set; } = string.Empty;
    public string[] Aliases { get; set; } = Array.Empty<string>();
    public bool RequiresConfirmation { get; set; }
    public string Category { get; set; } = string.Empty;
    
    public string DisplayName => Command;
    
    public string? Description { get; set; }
    
    public bool IsAvailable { get; set; } = true;
    
    public string Value => Command;
    
    public string? ExtraInfo => 
        $"{Category}{(Aliases.Length > 0 ? $" | Aliases: {string.Join(", ", Aliases)}" : "")}";
}