using Microsoft.Extensions.Logging;

namespace Deck.Core.Interfaces;

/// <summary>
/// 日志服务接口 - 提供统一的日志管理
/// 基于 .NET 9 ILogger 实现，支持结构化日志和性能优化
/// </summary>
public interface ILoggingService
{
    /// <summary>
    /// 获取指定类型的日志记录器
    /// </summary>
    ILogger<T> GetLogger<T>();

    /// <summary>
    /// 获取指定名称的日志记录器
    /// </summary>
    ILogger GetLogger(string categoryName);

    /// <summary>
    /// 配置日志级别
    /// </summary>
    void SetLogLevel(string categoryName, LogLevel logLevel);

    /// <summary>
    /// 启用/禁用控制台彩色输出
    /// </summary>
    void EnableConsoleColors(bool enabled);

    /// <summary>
    /// 获取当前日志配置
    /// </summary>
    LoggingConfiguration GetCurrentConfiguration();
}

/// <summary>
/// 日志配置信息
/// </summary>
public class LoggingConfiguration
{
    /// <summary>
    /// 默认日志级别
    /// </summary>
    public LogLevel DefaultLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// 是否启用控制台颜色
    /// </summary>
    public bool EnableConsoleColors { get; set; } = true;

    /// <summary>
    /// 是否启用时间戳
    /// </summary>
    public bool EnableTimestamps { get; set; } = true;

    /// <summary>
    /// 日志格式模式
    /// </summary>
    public string? FormatPattern { get; set; }

    /// <summary>
    /// 分类级别配置
    /// </summary>
    public Dictionary<string, LogLevel> CategoryLevels { get; set; } = new();
}