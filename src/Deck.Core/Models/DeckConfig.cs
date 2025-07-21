namespace Deck.Core.Models;

/// <summary>
/// Deck配置根对象，对应.deck/config.yaml文件结构
/// </summary>
public class DeckConfig
{
    /// <summary>
    /// 模板管理配置
    /// </summary>
    public TemplateConfig Templates { get; set; } = new();

    /// <summary>
    /// 容器引擎配置
    /// </summary>
    public ContainerEngineConfig Container { get; set; } = new();

    /// <summary>
    /// 网络配置
    /// </summary>
    public NetworkConfig Network { get; set; } = new();

    /// <summary>
    /// 项目配置
    /// </summary>
    public ProjectConfig Project { get; set; } = new();

    /// <summary>
    /// 缓存配置
    /// </summary>
    public CacheConfig Cache { get; set; } = new();

    /// <summary>
    /// 用户界面配置
    /// </summary>
    public UIConfig UI { get; set; } = new();

    /// <summary>
    /// 开发配置
    /// </summary>
    public DevelopmentConfig Development { get; set; } = new();

    /// <summary>
    /// 安全配置
    /// </summary>
    public SecurityConfig Security { get; set; } = new();

    /// <summary>
    /// 日志配置
    /// </summary>
    public LoggingConfig Logging { get; set; } = new();
}

/// <summary>
/// 模板管理配置
/// </summary>
public class TemplateConfig
{
    /// <summary>
    /// 仓库配置
    /// </summary>
    public RepositoryConfig Repository { get; set; } = new();

    /// <summary>
    /// 自动更新
    /// </summary>
    public bool AutoUpdate { get; set; } = true;

    /// <summary>
    /// 缓存过期时间
    /// </summary>
    public string CacheExpire { get; set; } = "24h";

    /// <summary>
    /// 启动时更新
    /// </summary>
    public bool UpdateOnStart { get; set; } = true;
}

/// <summary>
/// 仓库配置
/// </summary>
public class RepositoryConfig
{
    /// <summary>
    /// 仓库URL
    /// </summary>
    public string Url { get; set; } = "https://github.com/chatterzhao/deck-templates.git";

    /// <summary>
    /// 分支
    /// </summary>
    public string Branch { get; set; } = "main";

    /// <summary>
    /// 备用仓库URL
    /// </summary>
    public string FallbackUrl { get; set; } = "https://gitee.com/zhaoquan/deck-templates.git";
}

/// <summary>
/// 容器引擎配置
/// </summary>
public class ContainerEngineConfig
{
    /// <summary>
    /// 容器引擎类型（podman或docker）
    /// </summary>
    public string Engine { get; set; } = "podman";

    /// <summary>
    /// 自动安装
    /// </summary>
    public bool AutoInstall { get; set; } = true;

    /// <summary>
    /// 启动时检查
    /// </summary>
    public bool CheckOnStart { get; set; } = true;

    /// <summary>
    /// 备用引擎
    /// </summary>
    public string FallbackEngine { get; set; } = "docker";
}

/// <summary>
/// 网络配置
/// </summary>
public class NetworkConfig
{
    /// <summary>
    /// 代理配置
    /// </summary>
    public ProxyConfig Proxy { get; set; } = new();

    /// <summary>
    /// DNS配置
    /// </summary>
    public List<string> DNS { get; set; } = new() { "8.8.8.8", "1.1.1.1" };

    /// <summary>
    /// 镜像源配置
    /// </summary>
    public MirrorsConfig Mirrors { get; set; } = new();
}

/// <summary>
/// 代理配置
/// </summary>
public class ProxyConfig
{
    /// <summary>
    /// HTTP代理
    /// </summary>
    public string Http { get; set; } = string.Empty;

    /// <summary>
    /// HTTPS代理
    /// </summary>
    public string Https { get; set; } = string.Empty;

    /// <summary>
    /// 不使用代理的地址
    /// </summary>
    public string NoProxy { get; set; } = "localhost,127.0.0.1";
}

/// <summary>
/// 镜像源配置
/// </summary>
public class MirrorsConfig
{
    /// <summary>
    /// Docker镜像仓库镜像
    /// </summary>
    public string DockerRegistry { get; set; } = "docker.m.daocloud.io";

    /// <summary>
    /// APT镜像源
    /// </summary>
    public string AptMirror { get; set; } = "mirrors.ustc.edu.cn";
}

/// <summary>
/// 项目配置
/// </summary>
public class ProjectConfig
{
    /// <summary>
    /// 项目名称，为空时使用目录名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 工作空间路径
    /// </summary>
    public string WorkspacePath { get; set; } = "/workspace";

    /// <summary>
    /// 自动创建gitignore
    /// </summary>
    public bool AutoCreateGitignore { get; set; } = true;
}

/// <summary>
/// 缓存配置
/// </summary>
public class CacheConfig
{
    /// <summary>
    /// 启用构建缓存
    /// </summary>
    public bool EnableBuildCache { get; set; } = true;

    /// <summary>
    /// 缓存目录
    /// </summary>
    public string CacheDirectory { get; set; } = ".deck/cache";

    /// <summary>
    /// 最大缓存大小
    /// </summary>
    public string MaxCacheSize { get; set; } = "5GB";

    /// <summary>
    /// 自动清理
    /// </summary>
    public bool AutoCleanup { get; set; } = true;

    /// <summary>
    /// 清理天数
    /// </summary>
    public int CleanupDays { get; set; } = 30;
}

/// <summary>
/// 用户界面配置
/// </summary>
public class UIConfig
{
    /// <summary>
    /// 语言
    /// </summary>
    public string Language { get; set; } = "zh-CN";

    /// <summary>
    /// 显示提示
    /// </summary>
    public bool ShowTips { get; set; } = true;

    /// <summary>
    /// 交互模式
    /// </summary>
    public bool InteractiveMode { get; set; } = true;

    /// <summary>
    /// 显示Podman命令
    /// </summary>
    public bool ShowPodmanCommands { get; set; } = true;
}

/// <summary>
/// 开发配置
/// </summary>
public class DevelopmentConfig
{
    /// <summary>
    /// 自动端口转发
    /// </summary>
    public bool AutoPortForward { get; set; } = true;

    /// <summary>
    /// 默认内存限制
    /// </summary>
    public string DefaultMemoryLimit { get; set; } = "4g";

    /// <summary>
    /// 默认CPU限制
    /// </summary>
    public string DefaultCpuLimit { get; set; } = "2";

    /// <summary>
    /// 启用热重载
    /// </summary>
    public bool EnableHotReload { get; set; } = true;

    /// <summary>
    /// 显示构建输出
    /// </summary>
    public bool ShowBuildOutput { get; set; } = true;
}

/// <summary>
/// 安全配置
/// </summary>
public class SecurityConfig
{
    /// <summary>
    /// 启用no-new-privileges
    /// </summary>
    public bool EnableNoNewPrivileges { get; set; } = true;

    /// <summary>
    /// 限制权限
    /// </summary>
    public bool RestrictedCapabilities { get; set; } = true;

    /// <summary>
    /// 扫描模板
    /// </summary>
    public bool ScanTemplates { get; set; } = false;
}

/// <summary>
/// 日志配置
/// </summary>
public class LoggingConfig
{
    /// <summary>
    /// 日志级别
    /// </summary>
    public string Level { get; set; } = "info";

    /// <summary>
    /// 日志文件
    /// </summary>
    public string File { get; set; } = ".deck/logs/deck.log";

    /// <summary>
    /// 最大文件大小
    /// </summary>
    public string MaxFileSize { get; set; } = "10MB";

    /// <summary>
    /// 保留文件数
    /// </summary>
    public int KeepFiles { get; set; } = 5;
}