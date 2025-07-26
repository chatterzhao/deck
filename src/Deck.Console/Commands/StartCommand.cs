using System.CommandLine;
using Deck.Core.Interfaces;
using Deck.Core.Models;
using Microsoft.Extensions.Logging;

namespace Deck.Console.Commands;

/// <summary>
/// 启动容器命令 - 专注于命令行参数处理和用户界面
/// </summary>
public class StartCommand : ContainerCommandBase
{
    private readonly IStartCommandService _startCommandService;
    private readonly IGlobalExceptionHandler _globalExceptionHandler;

    public StartCommand(
        IConsoleDisplay consoleDisplay,
        IInteractiveSelectionService interactiveSelectionService,
        ILoggingService loggingService,
        IDirectoryManagementService directoryManagement,
        IStartCommandService startCommandService,
        IGlobalExceptionHandler globalExceptionHandler)
        : base(consoleDisplay, interactiveSelectionService, loggingService, directoryManagement)
    {
        _startCommandService = startCommandService;
        _globalExceptionHandler = globalExceptionHandler;
    }

    /// <summary>
    /// 执行启动命令 - 专注于参数验证和结果展示
    /// </summary>
    public override async Task<bool> ExecuteAsync(string? envType, CancellationToken cancellationToken = default)
    {
        ILogger logger = LoggingService.GetLogger("Deck.Console.Start");

        try
        {
            // 1. 参数验证和日志记录
            logger.LogInformation("Start command called with env-type: {EnvType}", envType ?? "auto-detect");
            
            // 2. 调用核心服务执行业务逻辑
            StartCommandResult result = await _startCommandService.ExecuteAsync(envType, cancellationToken);

            // 3. 结果展示和用户反馈
            return DisplayResult(result);
        }
        catch (Exception ex)
        {
            // 使用全局异常处理服务处理异常
            var context = new ExceptionContext
            {
                CommandName = "Start",
                Operation = "执行Start命令",
                ResourcePath = envType
            };
            
            var result = await _globalExceptionHandler.HandleExceptionAsync(ex, context);
            return result.IsHandled;
        }
    }

    /// <summary>
    /// 显示执行结果
    /// </summary>
    private bool DisplayResult(StartCommandResult result)
    {
        if (result.IsSuccess)
        {
            ConsoleDisplay.ShowSuccess($"✅ 启动成功: {result.ImageName}");
            if (!string.IsNullOrEmpty(result.ContainerName))
            {
                ConsoleDisplay.ShowInfo($"📦 容器名称: {result.ContainerName}");
            }

            if (result.DevelopmentInfo != null)
            {
                ShowDevelopmentInfo(result.DevelopmentInfo);
            }

            return true;
        }
        else
        {
            ConsoleDisplay.ShowError($"❌ 启动失败: {result.ErrorMessage}");
            return false;
        }
    }

    /// <summary>
    /// 显示开发环境信息
    /// </summary>
    private void ShowDevelopmentInfo(DevelopmentInfo devInfo)
    {
        ConsoleDisplay.ShowInfo("📋 开发环境信息：");
        
        if (devInfo.DevPort > 0)
        {
            ConsoleDisplay.ShowInfo($"  🌐 开发服务：http://localhost:{devInfo.DevPort}");
        }
        
        if (devInfo.DebugPort > 0)
        {
            ConsoleDisplay.ShowInfo($"  🐛 调试端口：{devInfo.DebugPort}");
        }
        
        if (devInfo.WebPort > 0)
        {
            ConsoleDisplay.ShowInfo($"  📱 Web端口：http://localhost:{devInfo.WebPort}");
        }
    }
}