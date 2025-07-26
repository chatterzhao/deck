using Deck.Core.Interfaces;
using Deck.Core.Models;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;

namespace Deck.Services;

/// <summary>
/// 全局异常处理服务
/// </summary>
public class GlobalExceptionHandler : IGlobalExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IConsoleDisplay _consoleDisplay;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IConsoleDisplay consoleDisplay)
    {
        _logger = logger;
        _consoleDisplay = consoleDisplay;
    }

    /// <inheritdoc/>
    public async Task<GlobalExceptionResult> HandleExceptionAsync(Exception exception, ExceptionContext context)
    {
        var result = new GlobalExceptionResult
        {
            IsHandled = true,
            ExceptionType = ClassifyException(exception),
            TechnicalDetails = exception.ToString()
        };

        // 记录详细日志
        _logger.LogError(exception, "在执行 {Operation} 时发生异常，上下文: {@Context}", context.Operation, context);

        // 生成用户友好的错误消息
        result.UserMessage = GenerateUserFriendlyMessage(exception, context);
        
        // 提供解决方案建议
        result.SuggestedSolutions = GenerateSolutions(exception, context);
        
        // 尝试自动恢复
        result.RecoveryResult = await TryRecoverAsync(exception, context);
        result.CanAutoRecover = result.RecoveryResult?.IsRecovered ?? false;

        // 显示错误信息给用户
        DisplayErrorToUser(result);

        return result;
    }

    /// <inheritdoc/>
    public async Task<RecoveryResult> TryRecoverAsync(Exception exception, ExceptionContext context)
    {
        var recoveryResult = new RecoveryResult();
        
        try
        {
            switch (ClassifyException(exception))
            {
                case ExceptionType.PortConflict:
                    // 端口冲突通常需要用户手动解决
                    recoveryResult.IsRecovered = false;
                    recoveryResult.ActionDescription = "端口冲突需要手动解决";
                    break;
                    
                case ExceptionType.Permission:
                    // 权限问题可能需要用户手动处理
                    recoveryResult.IsRecovered = false;
                    recoveryResult.ActionDescription = "权限问题需要手动处理";
                    break;
                    
                case ExceptionType.Network:
                    // 网络问题可以尝试重连
                    recoveryResult.IsRecovered = await TryRecoverNetworkIssueAsync(exception, context);
                    recoveryResult.ActionDescription = "尝试网络连接恢复";
                    break;
                    
                case ExceptionType.FileSystem:
                    // 文件系统问题可以尝试修复
                    recoveryResult.IsRecovered = await TryRecoverFileSystemIssueAsync(exception, context);
                    recoveryResult.ActionDescription = "尝试文件系统恢复";
                    break;
                    
                default:
                    recoveryResult.IsRecovered = false;
                    recoveryResult.ActionDescription = "无法自动恢复该类型错误";
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "尝试自动恢复时发生错误");
            recoveryResult.IsRecovered = false;
            recoveryResult.Errors.Add($"恢复过程中发生错误: {ex.Message}");
        }

        return recoveryResult;
    }

    #region Private Methods

    /// <summary>
    /// 对异常进行分类
    /// </summary>
    private ExceptionType ClassifyException(Exception exception)
    {
        return exception switch
        {
            UnauthorizedAccessException or System.Security.SecurityException => ExceptionType.Permission,
            DirectoryNotFoundException or FileNotFoundException => ExceptionType.FileSystem,
            SocketException or System.Net.WebException => ExceptionType.Network,
            InvalidOperationException when exception.Message.Contains("port") => ExceptionType.PortConflict,
            InvalidOperationException when exception.Message.Contains("container") => ExceptionType.ContainerEngine,
            _ => ExceptionType.Unknown
        };
    }

    /// <summary>
    /// 生成用户友好的错误消息
    /// </summary>
    private string GenerateUserFriendlyMessage(Exception exception, ExceptionContext context)
    {
        var commandName = string.IsNullOrEmpty(context.CommandName) ? "操作" : context.CommandName;
        
        return ClassifyException(exception) switch
        {
            ExceptionType.Permission => $"执行 {commandName} 时权限不足，请检查相关文件或目录的访问权限",
            ExceptionType.FileSystem => $"执行 {commandName} 时文件系统出现问题，请检查相关文件或目录是否存在且可访问",
            ExceptionType.Network => $"执行 {commandName} 时网络连接出现问题，请检查网络连接状态",
            ExceptionType.PortConflict => $"执行 {commandName} 时检测到端口冲突，请检查端口占用情况",
            ExceptionType.ContainerEngine => $"执行 {commandName} 时容器引擎出现问题，请检查容器引擎是否正常运行",
            ExceptionType.Configuration => $"执行 {commandName} 时配置出现问题，请检查配置文件是否正确",
            _ => $"执行 {commandName} 时发生未知错误: {exception.Message}"
        };
    }

    /// <summary>
    /// 生成解决方案建议
    /// </summary>
    private List<string> GenerateSolutions(Exception exception, ExceptionContext context)
    {
        var solutions = new List<string>();
        
        switch (ClassifyException(exception))
        {
            case ExceptionType.Permission:
                solutions.Add("使用管理员权限运行程序");
                solutions.Add("检查相关文件或目录的访问权限");
                solutions.Add("在Linux/macOS上使用 chmod 命令修改权限");
                break;
                
            case ExceptionType.FileSystem:
                solutions.Add("检查相关文件或目录是否存在");
                solutions.Add("确保程序有足够权限访问相关路径");
                solutions.Add("检查磁盘空间是否充足");
                break;
                
            case ExceptionType.Network:
                solutions.Add("检查网络连接是否正常");
                solutions.Add("检查防火墙设置");
                solutions.Add("如果使用代理，请检查代理配置");
                break;
                
            case ExceptionType.PortConflict:
                solutions.Add("检查端口占用情况");
                solutions.Add("修改配置文件中的端口设置");
                solutions.Add("停止占用端口的进程");
                break;
                
            case ExceptionType.ContainerEngine:
                solutions.Add("检查容器引擎(Podman/Docker)是否正常运行");
                solutions.Add("尝试重启容器引擎服务");
                solutions.Add("检查容器引擎版本是否兼容");
                break;
                
            case ExceptionType.Configuration:
                solutions.Add("检查配置文件格式是否正确");
                solutions.Add("检查配置项是否完整");
                solutions.Add("参考文档确认配置文件结构");
                break;
                
            default:
                solutions.Add("查看详细日志以获取更多信息");
                solutions.Add("检查系统环境是否满足要求");
                solutions.Add("尝试重新启动程序");
                break;
        }
        
        return solutions;
    }

    /// <summary>
    /// 尝试恢复网络问题
    /// </summary>
    private async Task<bool> TryRecoverNetworkIssueAsync(Exception exception, ExceptionContext context)
    {
        // 这里可以实现网络问题的自动恢复逻辑
        // 例如：重试连接、切换镜像源等
        await Task.Delay(100); // 模拟处理时间
        return false; // 暂时返回false，实际实现中可以根据具体情况返回true
    }

    /// <summary>
    /// 尝试恢复文件系统问题
    /// </summary>
    private async Task<bool> TryRecoverFileSystemIssueAsync(Exception exception, ExceptionContext context)
    {
        // 这里可以实现文件系统问题的自动恢复逻辑
        // 例如：重新创建缺失的目录、恢复备份文件等
        await Task.Delay(100); // 模拟处理时间
        return false; // 暂时返回false，实际实现中可以根据具体情况返回true
    }

    /// <summary>
    /// 向用户显示错误信息
    /// </summary>
    private void DisplayErrorToUser(GlobalExceptionResult result)
    {
        _consoleDisplay.ShowError(result.UserMessage);
        
        if (result.SuggestedSolutions.Any())
        {
            _consoleDisplay.ShowInfo("\n💡 建议的解决方案:");
            for (int i = 0; i < result.SuggestedSolutions.Count; i++)
            {
                _consoleDisplay.WriteLine($"  {i + 1}. {result.SuggestedSolutions[i]}");
            }
        }
        
        if (result.CanAutoRecover && result.RecoveryResult != null)
        {
            if (result.RecoveryResult.IsRecovered)
            {
                _consoleDisplay.ShowSuccess($"\n✅ 已自动恢复: {result.RecoveryResult.ActionDescription}");
            }
            else
            {
                _consoleDisplay.ShowWarning($"\n⚠️  自动恢复失败: {result.RecoveryResult.ActionDescription}");
            }
        }
        
        // 在调试模式下显示详细技术信息
#if DEBUG
        _consoleDisplay.ShowWarning($"\n🔧 技术详情 (调试模式):");
        _consoleDisplay.WriteLine(result.TechnicalDetails);
#endif
    }

    #endregion
}