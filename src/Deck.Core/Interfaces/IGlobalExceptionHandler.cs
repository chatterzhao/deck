using Deck.Core.Models;

namespace Deck.Core.Interfaces;

/// <summary>
/// 全局异常处理服务接口
/// </summary>
public interface IGlobalExceptionHandler
{
    /// <summary>
    /// 处理异常
    /// </summary>
    /// <param name="exception">捕获的异常</param>
    /// <param name="context">异常上下文信息</param>
    /// <returns>异常处理结果</returns>
    Task<GlobalExceptionResult> HandleExceptionAsync(Exception exception, ExceptionContext context);
    
    /// <summary>
    /// 尝试自动恢复
    /// </summary>
    /// <param name="exception">异常信息</param>
    /// <param name="context">异常上下文</param>
    /// <returns>恢复结果</returns>
    Task<RecoveryResult> TryRecoverAsync(Exception exception, ExceptionContext context);
}

/// <summary>
/// 异常上下文信息
/// </summary>
public class ExceptionContext
{
    /// <summary>
    /// 命令名称
    /// </summary>
    public string? CommandName { get; set; }
    
    /// <summary>
    /// 操作描述
    /// </summary>
    public string? Operation { get; set; }
    
    /// <summary>
    /// 相关资源路径
    /// </summary>
    public string? ResourcePath { get; set; }
    
    /// <summary>
    /// 用户数据
    /// </summary>
    public Dictionary<string, object> UserData { get; set; } = new();
    
    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}