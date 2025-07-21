namespace Deck.Core.Models;

/// <summary>
/// 系统信息 - 对应 deck-shell 的系统检测结果
/// </summary>
public class SystemInfo
{
    /// <summary>
    /// 操作系统类型
    /// </summary>
    public OperatingSystemType OperatingSystem { get; set; }

    /// <summary>
    /// 系统架构
    /// </summary>
    public SystemArchitecture Architecture { get; set; }

    /// <summary>
    /// 系统版本
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 是否为 WSL 环境
    /// </summary>
    public bool IsWsl { get; set; }

    /// <summary>
    /// 可用内存 (MB)
    /// </summary>
    public long AvailableMemoryMb { get; set; }

    /// <summary>
    /// 可用磁盘空间 (GB)
    /// </summary>
    public long AvailableDiskSpaceGb { get; set; }
}

/// <summary>
/// 操作系统类型枚举
/// </summary>
public enum OperatingSystemType
{
    Windows,
    Linux,
    MacOS,
    Unknown
}

/// <summary>
/// 系统架构枚举
/// </summary>
public enum SystemArchitecture
{
    X64,
    ARM64,
    X86,
    Unknown
}

/// <summary>
/// 容器引擎信息
/// </summary>
public class ContainerEngineInfo
{
    /// <summary>
    /// 容器引擎类型
    /// </summary>
    public ContainerEngineType Type { get; set; }

    /// <summary>
    /// 引擎版本
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 是否可用
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// 安装路径
    /// </summary>
    public string? InstallPath { get; set; }

    /// <summary>
    /// 检测错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 容器引擎类型枚举
/// </summary>
public enum ContainerEngineType
{
    Podman,
    Docker,
    None
}

/// <summary>
/// 项目类型信息
/// </summary>
public class ProjectTypeInfo
{
    /// <summary>
    /// 检测到的项目类型
    /// </summary>
    public List<ProjectType> DetectedTypes { get; set; } = new();

    /// <summary>
    /// 推荐的主要项目类型
    /// </summary>
    public ProjectType? RecommendedType { get; set; }

    /// <summary>
    /// 项目根目录
    /// </summary>
    public string ProjectRoot { get; set; } = string.Empty;
}

/// <summary>
/// 项目类型枚举
/// </summary>
public enum ProjectType
{
    Tauri,
    Flutter,
    Avalonia,
    DotNet,
    Python,
    Node,
    Unknown
}

/// <summary>
/// 系统要求检查结果
/// </summary>
public class SystemRequirementsResult
{
    /// <summary>
    /// 是否满足系统要求
    /// </summary>
    public bool MeetsRequirements { get; set; }

    /// <summary>
    /// 检查结果列表
    /// </summary>
    public List<RequirementCheck> Checks { get; set; } = new();

    /// <summary>
    /// 警告信息
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// 单项要求检查
/// </summary>
public class RequirementCheck
{
    /// <summary>
    /// 检查项目名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 是否通过
    /// </summary>
    public bool Passed { get; set; }

    /// <summary>
    /// 检查结果描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 修复建议
    /// </summary>
    public string? Suggestion { get; set; }
}