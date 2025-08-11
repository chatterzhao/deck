using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Deck.Core.Interfaces;
using Deck.Core.Models;
using Microsoft.Extensions.Logging;

namespace Deck.Services;

/// <summary>
/// Start 命令服务的简化实现，专注于核心三层选择功能
/// </summary>
public class StartCommandServiceSimple : IStartCommandService
{
    private readonly ILogger<StartCommandServiceSimple> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IConsoleUIService _consoleUIService;
    private readonly IEnhancedFileOperationsService _enhancedFileOperationsService;
    private readonly IConfigurationService _configurationService;
    private readonly IRemoteTemplatesService _remoteTemplatesService;
    private readonly IFileSystemService _fileSystemService;

    // 目录常量
    private const string DeckDir = ".deck";
    private const string ImagesDir = ".deck/images";
    private const string CustomDir = ".deck/custom";
    private const string TemplatesDir = ".deck/templates";

    public StartCommandServiceSimple(
        ILogger<StartCommandServiceSimple> logger,
        ILoggerFactory loggerFactory,
        IConsoleUIService consoleUIService,
        IEnhancedFileOperationsService enhancedFileOperationsService,
        IConfigurationService configurationService,
        IRemoteTemplatesService remoteTemplatesService,
        IFileSystemService fileSystemService)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _consoleUIService = consoleUIService;
        _enhancedFileOperationsService = enhancedFileOperationsService;
        _configurationService = configurationService;
        _remoteTemplatesService = remoteTemplatesService;
        _fileSystemService = fileSystemService;
    }

    public async Task<StartCommandResult> ExecuteAsync(string? envType, CancellationToken cancellationToken = default)
    {
        try
        {
            // 只在必要时显示信息，而不是总是显示
            //_consoleUIService.ShowInfo("🚀 启动容器化工具...");

            // 初始化目录结构
            InitializeDirectoryStructure();
            
            // 确保配置文件存在
            await EnsureConfigurationAsync(cancellationToken);
            
            // 更新模板目录
            var templateSyncResult = await UpdateTemplatesAsync(cancellationToken);

            // 获取三层配置选项
            var options = await GetOptionsAsync(envType, cancellationToken);

            // 检查是否有可用的模板选项
            if (templateSyncResult != null && !templateSyncResult.Success && options.Templates.Count == 0)
            {
                _consoleUIService.ShowError("❌ 模板同步失败且没有可用的本地模板");
                _consoleUIService.ShowInfo("💡 请检查网络连接或手动添加模板到 .deck/templates 目录");
                return StartCommandResult.Failure("模板不可用");
            }

            // 显示选择界面
            var selectedOption = _consoleUIService.ShowThreeLayerSelection(options);
            if (selectedOption == null)
            {
                return StartCommandResult.Failure("用户取消了选择");
            }

            // 根据选择类型执行对应操作
            return selectedOption.Type switch
            {
                OptionType.Image => await StartFromImageAsync(selectedOption.Name, cancellationToken),
                OptionType.Config => await StartFromConfigAsync(selectedOption.Name, cancellationToken),
                OptionType.Template => await HandleTemplateSelectionAsync(selectedOption.Name, options.EnvType, cancellationToken),
                _ => StartCommandResult.Failure("未知的选择类型")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Start command execution failed");
            return StartCommandResult.Failure($"执行失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 确保配置文件存在，如果不存在则创建默认配置
    /// </summary>
    private async Task EnsureConfigurationAsync(CancellationToken cancellationToken)
    {
        try
        {
            var config = await _configurationService.GetConfigAsync();
            // 减少冗余日志输出，只在调试时需要
            //_logger.LogInformation("配置文件已加载或创建: Repository={Repository}, Branch={Branch}", 
            //    config.RemoteTemplates.Repository, config.RemoteTemplates.Branch);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "确保配置文件存在时发生错误");
            throw new InvalidOperationException("无法创建或加载配置文件", ex);
        }
    }

    /// <summary>
    /// 更新模板目录内容
    /// </summary>
    private async Task<SyncResult?> UpdateTemplatesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var config = await _configurationService.GetConfigAsync();
            if (config.RemoteTemplates.AutoUpdate)
            {
                var syncResult = await _remoteTemplatesService.SyncTemplatesAsync(forceUpdate: false);
                if (syncResult.Success)
                {
                    // 只显示关键信息
                    _consoleUIService.ShowInfo($"✅ 从 {config.RemoteTemplates.Repository} 同步了 {syncResult.SyncedTemplateCount} 个模板");
                }
                else
                {
                    _consoleUIService.ShowWarning("⚠️ 模板同步失败: " + string.Join(", ", syncResult.SyncLogs));
                }
                
                return syncResult;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新模板时发生错误");
            _consoleUIService.ShowWarning("⚠️ 模板更新失败: " + ex.Message);
        }
        
        return null;
    }

    private void InitializeDirectoryStructure()
    {
        Directory.CreateDirectory(DeckDir);
        Directory.CreateDirectory(ImagesDir);
        Directory.CreateDirectory(CustomDir);
        Directory.CreateDirectory(TemplatesDir);
    }

    public Task<StartCommandThreeLayerOptions> GetOptionsAsync(string? envType, CancellationToken cancellationToken = default)
    {
        var options = new StartCommandThreeLayerOptions();

        // 环境类型处理
        if (string.IsNullOrEmpty(envType))
        {
            var projectType = DetectProjectEnvironment();
            options.EnvType = projectType ?? "unknown";
            options.IsAutoDetected = true;
            
            // 只在检测到环境时显示信息
            if (!string.IsNullOrEmpty(projectType))
            {
                _consoleUIService.ShowInfo($"🔍 检测到环境类型：{projectType}");
                //_consoleUIService.ShowWarning("💡 推荐选择对应的环境类型配置");
            }
        }
        else
        {
            options.EnvType = envType;
            options.IsAutoDetected = false;
            
            /*if (envType == "unknown")
            {
                _consoleUIService.ShowInfo("🔍 显示所有可用配置选项");
                _consoleUIService.ShowWarning("💡 提示：使用 'deck start <类型>' 可过滤特定环境，如 'deck start tauri'");
            }
            else
            {
                _consoleUIService.ShowInfo($"🔍 仅显示 {envType}- 开头的目录");
            }*/
        }

        // 加载三层配置
        options.Images = LoadImageOptions(options.EnvType);
        options.Configs = LoadConfigOptions(options.EnvType);
        options.Templates = LoadTemplateOptions(options.EnvType);

        return Task.FromResult(options);
    }

    public async Task<StartCommandResult> StartFromImageAsync(string imageName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting from image: {ImageName}", imageName);

        var imagePath = Path.Combine(ImagesDir, imageName);
        if (!Directory.Exists(imagePath))
        {
            return StartCommandResult.Failure($"镜像目录不存在: {imagePath}");
        }

        var envFilePath = Path.Combine(imagePath, ".env");
        if (!File.Exists(envFilePath))
        {
            return StartCommandResult.Failure($"环境文件不存在: {envFilePath}");
        }

        _consoleUIService.ShowInfo($"🚀 启动镜像: {imageName}");
        
        try
        {
            // 检查容器引擎是否可用
            _consoleUIService.ShowInfo("🔍 检查容器引擎...");
            var systemService = new SystemDetectionService(_loggerFactory.CreateLogger<SystemDetectionService>());
            var containerEngineInfo = await systemService.DetectContainerEngineAsync();
            
            // 如果容器引擎不可用但错误信息提到 Podman machine，尝试自动初始化
            if (!containerEngineInfo.IsAvailable && 
                containerEngineInfo.Type == ContainerEngineType.Podman && 
                !string.IsNullOrEmpty(containerEngineInfo.ErrorMessage) &&
                containerEngineInfo.ErrorMessage.Contains("machine"))
            {
                _consoleUIService.ShowInfo("🔧 检测到 Podman machine 未运行，尝试自动初始化...");
                Console.WriteLine("🔧 [调试] 检测到 Podman machine 未运行，尝试自动初始化...");
                Console.WriteLine($"🔧 [调试] 错误信息: {containerEngineInfo.ErrorMessage}");
                var initResult = await systemService.TryInitializePodmanMachineAsync();
                if (initResult)
                {
                    _consoleUIService.ShowSuccess("✅ Podman machine 初始化成功");
                    Console.WriteLine("✅ [调试] Podman machine 初始化成功");
                    // 重新检测容器引擎
                    containerEngineInfo = await systemService.DetectContainerEngineAsync();
                }
                else
                {
                    _consoleUIService.ShowWarning("⚠️ Podman machine 自动初始化失败，请手动运行: podman machine init && podman machine start");
                    Console.WriteLine("⚠️ [调试] Podman machine 自动初始化失败");
                }
            }
            
            // 检查是否需要重新安装（例如brew安装的情况）
            if (!await CheckAndHandlePodmanReinstallationAsync())
            {
                return StartCommandResult.Failure("Podman重新安装失败");
            }
            
            // 重新检测容器引擎
            containerEngineInfo = await systemService.DetectContainerEngineAsync();
            
            if (containerEngineInfo.Type == ContainerEngineType.None)
            {
                // 尝试安装容器引擎
                var installResult = await InstallContainerEngineAsync();
                if (!installResult)
                {
                    return StartCommandResult.Failure("未检测到可用的容器引擎，且自动安装失败");
                }
                
                // 重新检测
                containerEngineInfo = await systemService.DetectContainerEngineAsync();
                if (containerEngineInfo.Type == ContainerEngineType.None)
                {
                    return StartCommandResult.Failure("容器引擎安装后仍无法检测到，请手动检查");
                }
            }
            
            var engineName = containerEngineInfo.Type == ContainerEngineType.Podman ? "Podman" : "Docker";

            // 处理标准端口管理和冲突检测（仅检测，不修改文件）
            _consoleUIService.ShowInfo("🔍 检查端口配置和冲突...");
            var detectionOptions = new EnhancedFileOperationOptions { CreateBackup = false };
            var portResult = await _enhancedFileOperationsService.ProcessStandardPortsAsync(envFilePath, detectionOptions);
            if (!portResult.IsSuccess)
            {
                return StartCommandResult.Failure($"端口处理失败: {portResult.ErrorMessage}");
            }
            
            // 显示端口冲突解决信息并处理用户交互
            if (portResult.ModifiedPorts.Count > 0)
            {
                _consoleUIService.ShowWarning("⚠️ 检测到端口冲突：");
                foreach (var (portVar, newPort) in portResult.ModifiedPorts)
                {
                    _consoleUIService.ShowInfo($"  📌 {portVar}: 建议更改为端口 {newPort}");
                }
                
                // 询问用户是否要应用推荐的端口更改
                var applyPortChanges = _consoleUIService.ShowConfirmation("是否应用推荐的端口更改？");
                if (!applyPortChanges)
                {
                    return StartCommandResult.Failure("用户取消了启动，请检查端口配置后重试");
                }
                
                // 用户确认后才应用端口更改（这次会修改文件）
                var updateOptions = new EnhancedFileOperationOptions { CreateBackup = true };
                await _enhancedFileOperationsService.ProcessStandardPortsAsync(envFilePath, updateOptions);
            }
            
            // 显示其他端口警告
            foreach (var warning in portResult.Warnings.Where(w => !w.Contains("端口冲突：")))
            {
                _consoleUIService.ShowWarning($"⚠️ {warning}");
            }

            // 更新 PROJECT_NAME 避免容器名冲突
            _consoleUIService.ShowInfo("🏷️ 更新项目名称...");
            var projectNameResult = await _enhancedFileOperationsService.UpdateProjectNameAsync(envFilePath, imageName);
            if (!projectNameResult.IsSuccess)
            {
                _logger.LogWarning("PROJECT_NAME更新失败: {Error}", projectNameResult.ErrorMessage);
            }
            
            var containerName = $"{projectNameResult.UpdatedProjectName ?? imageName}-dev";
            
            // 显示开发环境信息（模拟deck-shell的行为）
            DisplayDevelopmentInfo(portResult.AllPorts);
            
            // 直接启动容器（因为是从已有镜像启动）
            var startSuccess = await StartExistingContainerAsync(containerName, engineName.ToLower(), cancellationToken);
            if (!startSuccess)
            {
                return StartCommandResult.Failure("容器启动失败");
            }

            return StartCommandResult.Success(imageName, containerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Starting image failed: {ImageName}", imageName);
            return StartCommandResult.Failure($"启动失败: {ex.Message}");
        }
    }

    public async Task<StartCommandResult> StartFromConfigAsync(string configName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting from config: {ConfigName}", configName);

        var configPath = Path.Combine(CustomDir, configName);
        if (!Directory.Exists(configPath))
        {
            return StartCommandResult.Failure($"配置目录不存在: {configPath}");
        }

        var envFilePath = Path.Combine(configPath, ".env");
        if (!File.Exists(envFilePath))
        {
            return StartCommandResult.Failure($"环境文件不存在: {envFilePath}");
        }

        _consoleUIService.ShowInfo($"🔨 从配置构建: {configName}");
        
        try
        {
            // 检查容器引擎是否可用
            _consoleUIService.ShowInfo("🔍 检查容器引擎...");
            var systemService = new SystemDetectionService(_loggerFactory.CreateLogger<SystemDetectionService>());
            var containerEngineInfo = await systemService.DetectContainerEngineAsync();
            
            // 如果容器引擎不可用但错误信息提到 Podman machine，尝试自动初始化
            if (!containerEngineInfo.IsAvailable && 
                containerEngineInfo.Type == ContainerEngineType.Podman && 
                !string.IsNullOrEmpty(containerEngineInfo.ErrorMessage) &&
                containerEngineInfo.ErrorMessage.Contains("machine"))
            {
                _consoleUIService.ShowInfo("🔧 检测到 Podman machine 未运行，尝试自动初始化...");
                var initResult = await systemService.TryInitializePodmanMachineAsync();
                if (initResult)
                {
                    _consoleUIService.ShowSuccess("✅ Podman machine 初始化成功");
                    // 重新检测容器引擎
                    containerEngineInfo = await systemService.DetectContainerEngineAsync();
                }
                else
                {
                    _consoleUIService.ShowWarning("⚠️ Podman machine 自动初始化失败，请手动运行: podman machine init && podman machine start");
                }
            }
            
            // 检查是否需要重新安装（例如brew安装的情况）
            if (!await CheckAndHandlePodmanReinstallationAsync())
            {
                return StartCommandResult.Failure("Podman重新安装失败");
            }
            
            // 重新检测容器引擎
            containerEngineInfo = await systemService.DetectContainerEngineAsync();
            
            if (containerEngineInfo.Type == ContainerEngineType.None)
            {
                // 尝试安装容器引擎
                var installResult = await InstallContainerEngineAsync();
                if (!installResult)
                {
                    return StartCommandResult.Failure("未检测到可用的容器引擎，且自动安装失败");
                }
                
                // 重新检测
                containerEngineInfo = await systemService.DetectContainerEngineAsync();
                if (containerEngineInfo.Type == ContainerEngineType.None)
                {
                    return StartCommandResult.Failure("容器引擎安装后仍无法检测到，请手动检查");
                }
            }
            
            var engineName = containerEngineInfo.Type == ContainerEngineType.Podman ? "Podman" : "Docker";

            // 处理标准端口管理和冲突检测（仅检测，不修改文件）
            _consoleUIService.ShowInfo("🔍 检查端口配置和冲突...");
            var detectionOptions = new EnhancedFileOperationOptions { CreateBackup = false };
            var portResult = await _enhancedFileOperationsService.ProcessStandardPortsAsync(envFilePath, detectionOptions);
            if (!portResult.IsSuccess)
            {
                return StartCommandResult.Failure($"端口处理失败: {portResult.ErrorMessage}");
            }
            
            // 显示端口冲突解决信息并处理用户交互
            if (portResult.ModifiedPorts.Count > 0)
            {
                _consoleUIService.ShowWarning("⚠️ 检测到端口冲突：");
                foreach (var (portVar, newPort) in portResult.ModifiedPorts)
                {
                    _consoleUIService.ShowInfo($"  📌 {portVar}: 建议更改为端口 {newPort}");
                }
                
                // 询问用户是否要应用推荐的端口更改
                var applyPortChanges = _consoleUIService.ShowConfirmation("是否应用推荐的端口更改？");
                if (!applyPortChanges)
                {
                    return StartCommandResult.Failure("用户取消了构建，请检查端口配置后重试");
                }
                
                // 用户确认后才应用端口更改（这次会修改文件）
                var updateOptions = new EnhancedFileOperationOptions { CreateBackup = true };
                await _enhancedFileOperationsService.ProcessStandardPortsAsync(envFilePath, updateOptions);
            }
            else
            {
                _consoleUIService.ShowSuccess("✅ 所有端口配置正常，无冲突");
            }
            
            // 显示其他端口警告
            foreach (var warning in portResult.Warnings.Where(w => !w.Contains("端口冲突：")))
            {
                _consoleUIService.ShowWarning($"⚠️ {warning}");
            }

            // 生成镜像名称（配置名称 + 时间戳）
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmm");
            var imageName = $"{configName}-{timestamp}";
            
            // 更新 PROJECT_NAME 避免容器名冲突
            _consoleUIService.ShowInfo("🏷️ 更新项目名称...");
            var projectNameResult = await _enhancedFileOperationsService.UpdateProjectNameAsync(envFilePath, imageName);
            if (!projectNameResult.IsSuccess)
            {
                _logger.LogWarning("PROJECT_NAME更新失败: {Error}", projectNameResult.ErrorMessage);
            }
            
            var containerName = $"{projectNameResult.UpdatedProjectName ?? imageName}-dev";
            
            // 显示开发环境信息
            DisplayDevelopmentInfo(portResult.AllPorts);
            
            // 实际执行构建和启动流程
            var buildResult = await BuildAndStartContainer(configName, imageName, containerName, engineName.ToLower(), cancellationToken);
            if (!buildResult.IsSuccess)
            {
                return buildResult;
            }

            return StartCommandResult.Success(imageName, containerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Starting from config failed: {ConfigName}", configName);
            return StartCommandResult.Failure($"启动失败: {ex.Message}");
        }
    }

    public async Task<StartCommandResult> StartFromTemplateAsync(string templateName, string? envType, TemplateWorkflowType workflowType, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting from template: {TemplateName}, workflow: {WorkflowType}", templateName, workflowType);

        var templatePath = Path.Combine(TemplatesDir, templateName);
        if (!Directory.Exists(templatePath))
        {
            return StartCommandResult.Failure($"模板目录不存在: {templatePath}");
        }

        try
        {
            // 检查容器引擎是否可用
            _consoleUIService.ShowInfo("🔍 检查容器引擎...");
            var systemService = new SystemDetectionService(_loggerFactory.CreateLogger<SystemDetectionService>());
            var containerEngineInfo = await systemService.DetectContainerEngineAsync();
            
            // 检查是否需要重新安装（例如brew安装的情况）
            if (!await CheckAndHandlePodmanReinstallationAsync())
            {
                return StartCommandResult.Failure("Podman重新安装失败");
            }
            
            // 重新检测容器引擎
            containerEngineInfo = await systemService.DetectContainerEngineAsync();
            
            if (containerEngineInfo.Type == ContainerEngineType.None)
            {
                // 尝试安装容器引擎
                var installResult = await InstallContainerEngineAsync();
                if (!installResult)
                {
                    return StartCommandResult.Failure("未检测到可用的容器引擎，且自动安装失败");
                }
                
                // 重新检测
                containerEngineInfo = await systemService.DetectContainerEngineAsync();
                if (containerEngineInfo.Type == ContainerEngineType.None)
                {
                    return StartCommandResult.Failure("容器引擎安装后仍无法检测到，请手动检查");
                }
            }
            
            var engineName = containerEngineInfo.Type == ContainerEngineType.Podman ? "Podman" : "Docker";

            if (workflowType == TemplateWorkflowType.CreateEditableConfig)
            {
                return await CreateEditableConfigFromTemplate(templateName, envType, cancellationToken);
            }
            else
            {
                return await DirectBuildFromTemplate(templateName, envType, engineName.ToLower(), cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Starting from template failed: {TemplateName}", templateName);
            return StartCommandResult.Failure($"启动失败: {ex.Message}");
        }
    }

    private async Task<StartCommandResult> HandleTemplateSelectionAsync(string templateName, string? envType, CancellationToken cancellationToken)
    {
        // 显示模板工作流程选择
        var workflowType = _consoleUIService.ShowTemplateWorkflowSelection();
        
        return await StartFromTemplateAsync(templateName, envType, workflowType, cancellationToken);
    }

    private async Task<StartCommandResult> CreateEditableConfigFromTemplate(string templateName, string? envType, CancellationToken cancellationToken)
    {
        _consoleUIService.ShowInfo("📝 创建可编辑配置：");

        // 生成配置名称（如果存在重复则添加序号）
        var configName = GenerateUniqueConfigName(templateName);
        
        if (configName != templateName)
        {
            _consoleUIService.ShowWarning($"💡 已有 {templateName}，本次创建为 {configName}");
        }

        var templatePath = Path.Combine(TemplatesDir, templateName);
        var configPath = Path.Combine(CustomDir, configName);

        // 复制模板目录到 custom 目录
        _consoleUIService.ShowInfo("📂 正在复制模板到 custom 目录...");
        await CopyDirectoryAsync(templatePath, configPath);
        _consoleUIService.ShowSuccess("✅ 模板复制完成");

        _consoleUIService.ShowSuccess("✅ 可编辑配置已创建完成");
        _consoleUIService.ShowWarning($"📁 配置位置: {configPath}");
        _consoleUIService.ShowInfo("📝 接下来您可以：");
        _consoleUIService.ShowInfo("  1. 编辑配置文件（.env, compose.yaml, Dockerfile）来自定义环境");
        _consoleUIService.ShowInfo("  2. 重新运行 'deck start' 并用【用户自定义配置 - Custom】区刚编辑过的配置的序号来构建启动");
        _consoleUIService.ShowWarning("💡 提示: 配置文件可自由修改，但请勿更改目录名称");

        return StartCommandResult.Success(configName, string.Empty);
    }

    private async Task<StartCommandResult> DirectBuildFromTemplate(string templateName, string? envType, string engine, CancellationToken cancellationToken)
    {
        _consoleUIService.ShowInfo("🚀 直接构建启动：");
        _consoleUIService.ShowInfo("📋 执行流程: 模板 → custom → images → 构建启动容器");

        // 生成配置名称
        var customName = GenerateUniqueConfigName(templateName);
        
        if (customName != templateName)
        {
            _consoleUIService.ShowWarning($"💡 已有 {templateName}，本次创建为 {customName}");
        }

        // 步骤 1: 创建 custom 配置
        _consoleUIService.ShowStep(1, 3, "创建 custom 配置");

        var templatePath = Path.Combine(TemplatesDir, templateName);
        var configPath = Path.Combine(CustomDir, customName);

        // 复制模板目录到 custom 目录
        _consoleUIService.ShowInfo("📂 正在复制模板到 custom 目录...");
        await CopyDirectoryAsync(templatePath, configPath);
        _consoleUIService.ShowSuccess("✅ 模板复制完成");

        // 步骤 2: 复制配置到 images 目录
        _consoleUIService.ShowStep(2, 3, "复制配置到 images 目录");
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmm");
        var imageName = $"{customName}-{timestamp}";

        var sourcePath = Path.Combine(CustomDir, customName);
        var targetPath = Path.Combine(ImagesDir, imageName);
        
        // 确保目标目录存在
        if (!Directory.Exists(targetPath))
        {
            Directory.CreateDirectory(targetPath);
        }
        
        // 复制整个目录
        await CopyDirectoryAsync(sourcePath, targetPath);
        _consoleUIService.ShowSuccess("✅ 配置复制完成");

        // 处理端口冲突和项目名称
        var envFilePath = Path.Combine(targetPath, ".env");
        if (File.Exists(envFilePath))
        {
            // 处理标准端口管理和冲突检测（仅检测，不修改文件）
            _consoleUIService.ShowInfo("🔍 检查端口配置和冲突...");
            var detectionOptions = new EnhancedFileOperationOptions { CreateBackup = false };
            var portResult = await _enhancedFileOperationsService.ProcessStandardPortsAsync(envFilePath, detectionOptions);
            if (!portResult.IsSuccess)
            {
                return StartCommandResult.Failure($"端口处理失败: {portResult.ErrorMessage}");
            }
            
            // 显示端口冲突解决信息并处理用户交互
            if (portResult.ModifiedPorts.Count > 0)
            {
                _consoleUIService.ShowWarning("⚠️ 检测到端口冲突：");
                foreach (var (portVar, newPort) in portResult.ModifiedPorts)
                {
                    _consoleUIService.ShowInfo($"  📌 {portVar}: 建议更改为端口 {newPort}");
                }
                
                // 询问用户是否要应用推荐的端口更改
                var applyPortChanges = _consoleUIService.ShowConfirmation("是否应用推荐的端口更改？");
                if (!applyPortChanges)
                {
                    return StartCommandResult.Failure("用户取消了构建，请检查端口配置后重试");
                }
                
                // 用户确认后才应用端口更改（这次会修改文件）
                var updateOptions = new EnhancedFileOperationOptions { CreateBackup = true };
                await _enhancedFileOperationsService.ProcessStandardPortsAsync(envFilePath, updateOptions);
            }
            else
            {
                _consoleUIService.ShowSuccess("✅ 所有端口配置正常，无冲突");
            }
            
            // 显示其他端口警告
            foreach (var warning in portResult.Warnings.Where(w => !w.Contains("端口冲突：")))
            {
                _consoleUIService.ShowWarning($"⚠️ {warning}");
            }

            // 更新 PROJECT_NAME 避免容器名冲突
            _consoleUIService.ShowInfo("🏷️ 更新项目名称...");
            var projectNameResult = await _enhancedFileOperationsService.UpdateProjectNameAsync(envFilePath, imageName);
            if (!projectNameResult.IsSuccess)
            {
                _logger.LogWarning("PROJECT_NAME更新失败: {Error}", projectNameResult.ErrorMessage);
            }
            
            // 显示开发环境信息
            DisplayDevelopmentInfo(portResult.AllPorts);
        }

        // 步骤 3: 构建并启动镜像
        _consoleUIService.ShowStep(3, 3, "构建并启动镜像");

        // 3. 构建并启动容器（使用docker-compose一步完成）
        _consoleUIService.ShowInfo("🔨 正在构建并启动容器...");
        var startSuccess = await StartContainerAsync(imageName, $"{imageName}-dev", engine, cancellationToken);
        if (!startSuccess)
        {
            return StartCommandResult.Failure("容器构建或启动失败");
        }
        _consoleUIService.ShowSuccess($"✅ 容器构建并启动成功: {imageName}-dev");

        return StartCommandResult.Success(imageName, $"{imageName}-dev");
    }

    /// <summary>
    /// 构建镜像并启动容器的实际实现
    /// </summary>
    private async Task<StartCommandResult> BuildAndStartContainer(string configName, string imageName, string containerName, string engine, CancellationToken cancellationToken)
    {
        try
        {
            // 1. 复制custom目录到images目录
            _consoleUIService.ShowInfo("📂 正在复制配置到 images 目录...");
            var sourcePath = Path.Combine(CustomDir, configName);
            var targetPath = Path.Combine(ImagesDir, imageName);
            
            // 确保目标目录存在
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }
            
            // 复制整个目录
            await CopyDirectoryAsync(sourcePath, targetPath);
            _consoleUIService.ShowSuccess("✅ 配置复制完成");

            // 2. 构建并启动容器（使用docker-compose一步完成）
            _consoleUIService.ShowInfo("🔨 正在构建并启动容器...");
            var startSuccess = await StartContainerAsync(imageName, containerName, engine, cancellationToken);
            if (!startSuccess)
            {
                return StartCommandResult.Failure("容器构建或启动失败");
            }
            _consoleUIService.ShowSuccess($"✅ 容器构建并启动成功: {containerName}");

            return StartCommandResult.Success(imageName, containerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "构建和启动容器时发生错误");
            return StartCommandResult.Failure($"构建或启动失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 复制目录的辅助方法
    /// </summary>
    private async Task CopyDirectoryAsync(string sourceDir, string targetDir)
    {
        var source = new DirectoryInfo(sourceDir);
        var target = new DirectoryInfo(targetDir);
        
        if (!source.Exists)
        {
            throw new DirectoryNotFoundException($"源目录不存在: {sourceDir}");
        }
        
        Directory.CreateDirectory(target.FullName);
        
        // 复制文件
        foreach (var file in source.GetFiles())
        {
            var targetFilePath = Path.Combine(target.FullName, file.Name);
            file.CopyTo(targetFilePath, true);
        }
        
        // 递归复制子目录
        foreach (var subDir in source.GetDirectories())
        {
            var targetSubDir = Path.Combine(target.FullName, subDir.Name);
            await CopyDirectoryAsync(subDir.FullName, targetSubDir);
        }
    }

    /// <summary>
    /// 构建镜像的辅助方法
    /// </summary>
    private async Task<bool> BuildImageAsync(string contextPath, string imageName, string engine, CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = engine,
                Arguments = $"compose build --no-cache",
                WorkingDirectory = contextPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            // 异步读取输出，避免死锁
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync(cancellationToken);

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                _logger.LogError("镜像构建失败: {Error}", error);
                _consoleUIService.ShowError($"镜像构建失败: {error}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行镜像构建命令时发生异常");
            _consoleUIService.ShowError($"镜像构建异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 启动容器的辅助方法
    /// </summary>
    private async Task<bool> StartContainerAsync(string imageName, string containerName, string engine, CancellationToken cancellationToken)
    {
        try
        {
            // 使用compose文件构建并启动容器
            var startInfo = new ProcessStartInfo
            {
                FileName = engine,
                Arguments = $"compose up -d --build",
                WorkingDirectory = Path.Combine(ImagesDir, imageName),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            // 异步读取输出，避免死锁
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync(cancellationToken);

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                _logger.LogError("容器启动失败: {Error}", error);
                _consoleUIService.ShowError($"容器启动失败: {error}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行容器启动命令时发生异常");
            _consoleUIService.ShowError($"容器启动异常: {ex.Message}");
            return false;
        }
    }

    private List<ImageOption> LoadImageOptions(string envType)
    {
        var options = new List<ImageOption>();

        if (!Directory.Exists(ImagesDir))
        {
            return options;
        }

        var imageDirectories = Directory.GetDirectories(ImagesDir);
        foreach (var imageDir in imageDirectories)
        {
            var imageName = Path.GetFileName(imageDir);
            
            // 环境类型过滤
            if (envType != "unknown" && !imageName.StartsWith($"{envType}-"))
            {
                continue;
            }

            var (isAvailable, missingFiles) = CheckConfigFiles(imageDir);
            var relativeTime = GetRelativeTimeForImage(imageDir);

            options.Add(new ImageOption
            {
                Name = imageName,
                Path = imageDir,
                RelativeTime = relativeTime,
                IsAvailable = isAvailable,
                UnavailableReason = !isAvailable ? $"缺 {string.Join(", ", missingFiles)}" : null
            });
        }

        return options;
    }

    private List<ConfigOption> LoadConfigOptions(string envType)
    {
        var options = new List<ConfigOption>();

        if (!Directory.Exists(CustomDir))
        {
            return options;
        }

        var configDirectories = Directory.GetDirectories(CustomDir);
        foreach (var configDir in configDirectories)
        {
            var configName = Path.GetFileName(configDir);
            
            // 环境类型过滤
            if (envType != "unknown" && !configName.StartsWith($"{envType}-"))
            {
                continue;
            }

            var (isAvailable, missingFiles) = CheckConfigFiles(configDir);

            options.Add(new ConfigOption
            {
                Name = configName,
                Path = configDir,
                IsAvailable = isAvailable,
                UnavailableReason = !isAvailable ? $"缺 {string.Join(", ", missingFiles)}" : null
            });
        }

        return options;
    }

    private List<TemplateOption> LoadTemplateOptions(string envType)
    {
        var options = new List<TemplateOption>();

        // 检查项目模板目录
        if (Directory.Exists(TemplatesDir))
        {
            var templateDirectories = Directory.GetDirectories(TemplatesDir);
            foreach (var templateDir in templateDirectories)
            {
                var templateName = Path.GetFileName(templateDir);
                
                // 环境类型过滤
                if (envType != "unknown" && !templateName.StartsWith($"{envType}-"))
                {
                    continue;
                }

                var (isAvailable, missingFiles) = CheckConfigFiles(templateDir);

                options.Add(new TemplateOption
                {
                    Name = templateName,
                    Path = templateDir,
                    IsBuiltIn = false,
                    IsAvailable = isAvailable,
                    UnavailableReason = !isAvailable ? $"缺 {string.Join(", ", missingFiles)}" : null
                });
            }
        }


        return options;
    }

    /// <summary>
    /// 启动已存在的容器
    /// </summary>
    private async Task<bool> StartExistingContainerAsync(string containerName, string engine, CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = engine,
                Arguments = $"start {containerName}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            // 异步读取输出，避免死锁
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync(cancellationToken);

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                _logger.LogError("容器启动失败: {Error}", error);
                _consoleUIService.ShowError($"容器启动失败: {error}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行容器启动命令时发生异常");
            _consoleUIService.ShowError($"容器启动异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 尝试安装容器引擎
    /// </summary>
    private async Task<bool> InstallContainerEngineAsync()
    {
        _consoleUIService.ShowWarning("⚠️ 未检测到容器引擎");
        _consoleUIService.ShowInfo("💡 Deck需要Podman或Docker来运行容器");
        
        // 检查操作系统并提供适当建议
        var systemService = new SystemDetectionService(_loggerFactory.CreateLogger<SystemDetectionService>());
        var systemInfo = await systemService.GetSystemInfoAsync();
        
        if (systemInfo.OperatingSystem == OperatingSystemType.MacOS)
        {
            _consoleUIService.ShowInfo("💡 macOS用户建议：");
            _consoleUIService.ShowInfo("  1. 从 https://podman.io/downloads 下载官方安装包（推荐）");
            _consoleUIService.ShowInfo("  2. 或使用包管理器安装（如Homebrew，但可能有稳定性问题）");
        }
        
        var install = _consoleUIService.ShowConfirmation("是否尝试自动安装Podman？");
        if (!install)
        {
            _consoleUIService.ShowInfo("💡 您可以选择手动安装Podman或Docker");
            if (systemInfo.OperatingSystem == OperatingSystemType.MacOS)
            {
                _consoleUIService.ShowInfo("💡 macOS推荐从 https://podman.io/downloads 下载官方安装包");
            }
            return false;
        }

        // 执行Podman安装
        _consoleUIService.ShowInfo("🔧 正在尝试安装Podman...");
        var installSuccess = await InstallPodmanEngineAsync();
        
        if (installSuccess)
        {
            _consoleUIService.ShowSuccess("✅ Podman安装成功");
            
            // 初始化Podman Machine（仅限macOS/Windows)
            if (systemInfo.OperatingSystem != OperatingSystemType.Linux)
            {
                _consoleUIService.ShowInfo("⚙️ 初始化 Podman Machine...");
                await InitializePodmanMachineAsync();
            }
            
            _consoleUIService.ShowSuccess("✅ Podman环境准备就绪");
            return true;
        }
        else
        {
            _consoleUIService.ShowError("❌ Podman安装失败");
            _consoleUIService.ShowInfo("💡 建议手动从 https://podman.io/downloads 下载并安装Podman");
            return false;
        }
    }
    
    /// <summary>
    /// 检查是否需要重新安装Podman（例如从brew安装的情况）
    /// </summary>
    private async Task<bool> CheckAndHandlePodmanReinstallationAsync()
    {
        var systemService = new SystemDetectionService(_loggerFactory.CreateLogger<SystemDetectionService>());
        var containerEngineInfo = await systemService.DetectContainerEngineAsync();
        
        // 检查Podman是否通过brew安装
        if (containerEngineInfo.Type == ContainerEngineType.Podman && 
            !string.IsNullOrEmpty(containerEngineInfo.InstallPath) &&
            containerEngineInfo.InstallPath.Contains("brew"))
        {
            _consoleUIService.ShowWarning("⚠️ 检测到Podman通过Homebrew安装");
            _consoleUIService.ShowInfo("💡 Podman官方不推荐通过Homebrew安装，可能存在稳定性问题");
            _consoleUIService.ShowInfo("💡 建议卸载brew版本并安装官方版本以获得更好的体验");
            
            var reinstall = _consoleUIService.ShowConfirmation("是否卸载当前版本并重新安装官方版本？");
            if (reinstall)
            {
                // 尝试卸载brew版本
                _consoleUIService.ShowInfo("🔧 正在卸载brew版本的Podman...");
                var uninstallSuccess = await UninstallBrewPodmanAsync();
                if (uninstallSuccess)
                {
                    _consoleUIService.ShowSuccess("✅ 已卸载brew版本的Podman");
                    return await InstallContainerEngineAsync();
                }
                else
                {
                    _consoleUIService.ShowError("❌ 卸载brew版本的Podman失败");
                    return false;
                }
            }
        }
        
        return true; // 不需要重新安装
    }
    
    /// <summary>
    /// 卸载通过brew安装的Podman
    /// </summary>
    private async Task<bool> UninstallBrewPodmanAsync()
    {
        try
        {
            using var process = new Process();
            process.StartInfo.FileName = "/bin/bash";
            process.StartInfo.Arguments = "-c \"brew uninstall podman\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.Start();
            await process.WaitForExitAsync();

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "卸载brew版本的Podman时发生异常");
            return false;
        }
    }

    /// <summary>
    /// 获取Podman安装命令
    /// </summary>
    private PodmanInstallCommand? GetPodmanInstallCommand(SystemInfo systemInfo)
    {
        return systemInfo.OperatingSystem switch
        {
            OperatingSystemType.MacOS => GetMacOSInstallCommand(),
            OperatingSystemType.Linux => GetLinuxInstallCommand(),
            OperatingSystemType.Windows => GetWindowsInstallCommand(),
            _ => null
        };
    }

    /// <summary>
    /// 获取macOS安装命令
    /// </summary>
    private PodmanInstallCommand? GetMacOSInstallCommand()
    {
        // 优先检查 Homebrew
        if (IsCommandAvailable("brew"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "Homebrew",
                Command = "brew install podman",
                RequiresAdmin = false
            };
        }

        // 检查 MacPorts
        if (IsCommandAvailable("port"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "MacPorts",
                Command = "sudo port install podman",
                RequiresAdmin = true
            };
        }

        // 如果没有包管理器，则提供从官网下载pkg安装包的选项
        return new PodmanInstallCommand
        {
            PackageManager = "PKG Installer",
            Command = "download_and_install_podman_pkg",
            RequiresAdmin = true
        };
    }

    /// <summary>
    /// 获取Linux安装命令
    /// </summary>
    private PodmanInstallCommand? GetLinuxInstallCommand()
    {
        // APT (Ubuntu/Debian)
        if (IsCommandAvailable("apt"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "APT",
                Command = "sudo apt update && sudo apt install -y podman",
                RequiresAdmin = true
            };
        }

        // DNF (Fedora)
        if (IsCommandAvailable("dnf"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "DNF",
                Command = "sudo dnf install -y podman",
                RequiresAdmin = true
            };
        }

        // YUM (CentOS/RHEL)
        if (IsCommandAvailable("yum"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "YUM",
                Command = "sudo yum install -y podman",
                RequiresAdmin = true
            };
        }

        // Zypper (openSUSE)
        if (IsCommandAvailable("zypper"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "Zypper",
                Command = "sudo zypper install -y podman",
                RequiresAdmin = true
            };
        }

        // Pacman (Arch Linux)
        if (IsCommandAvailable("pacman"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "Pacman",
                Command = "sudo pacman -S --noconfirm podman",
                RequiresAdmin = true
            };
        }

        return null;
    }

    /// <summary>
    /// 获取Windows安装命令
    /// </summary>
    private PodmanInstallCommand? GetWindowsInstallCommand()
    {
        // Chocolatey
        if (IsCommandAvailable("choco"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "Chocolatey",
                Command = "choco install podman-desktop -y",
                RequiresAdmin = true,
                WarningMessage = "注意：将通过Chocolatey安装Podman Desktop"
            };
        }

        // Scoop
        if (IsCommandAvailable("scoop"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "Scoop",
                Command = "scoop install podman",
                RequiresAdmin = false,
                WarningMessage = "注意：将通过Scoop安装Podman"
            };
        }

        // WinGet
        if (IsCommandAvailable("winget"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "WinGet",
                Command = "winget install RedHat.Podman",
                RequiresAdmin = false,
                WarningMessage = "注意：将通过WinGet安装Podman"
            };
        }

        // 如果没有包管理器，则提供从GitHub下载MSI安装包的选项
        return new PodmanInstallCommand
        {
            PackageManager = "MSI Installer",
            Command = "download_and_install_podman_msi",
            RequiresAdmin = true,
            WarningMessage = "注意：将从GitHub下载Podman MSI安装包并安装"
        };
    }
    
    /// <summary>
    /// 安装Podman引擎
    /// </summary>
    private async Task<bool> InstallPodmanEngineAsync()
    {
        try
        {
            var systemService = new SystemDetectionService(_loggerFactory.CreateLogger<SystemDetectionService>());
            var systemInfo = await systemService.GetSystemInfoAsync();
            var installCommand = GetPodmanInstallCommand(systemInfo);

            if (installCommand == null)
            {
                _consoleUIService.ShowError("❌ 当前系统不支持自动安装 Podman");
                return false;
            }

            // 特殊处理直接下载安装包的方式（macOS和Windows）
            if (installCommand.Command.StartsWith("download_and_install_podman_"))
            {
                return await DownloadAndInstallPodmanPackageAsync(systemInfo);
            }

            // 执行包管理器安装命令
            using var process = new Process();
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = $"/c {installCommand.Command}";
            }
            else
            {
                process.StartInfo.FileName = "/bin/bash";
                process.StartInfo.Arguments = $"-c \"{installCommand.Command}\"";
            }

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            _logger.LogInformation("执行安装命令: {Command}", installCommand.Command);

            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode == 0)
            {
                _logger.LogInformation("Podman安装成功");
                return true;
            }
            else
            {
                _logger.LogError("Podman安装失败，退出码: {ExitCode}", process.ExitCode);
                _consoleUIService.ShowError($"安装失败 (退出码: {process.ExitCode})");
                if (!string.IsNullOrEmpty(error))
                {
                    _consoleUIService.ShowError($"错误信息: {error}");
                }
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行Podman安装命令时发生异常");
            _consoleUIService.ShowError($"安装过程中出现异常: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 统一下载并安装Podman安装包（支持macOS PKG和Windows MSI）
    /// </summary>
    private async Task<bool> DownloadAndInstallPodmanPackageAsync(SystemInfo systemInfo)
    {
        try
        {
            _consoleUIService.ShowInfo("🔍 正在获取最新Podman版本信息...");
            
            // 获取最新版本信息（简化处理，实际应该通过API获取）
            var latestVersion = "5.5.1"; // 这里应该通过API动态获取
            var architecture = GetSystemArchitectureString(systemInfo.Architecture);
            
            // 构造下载URL和文件路径
            string downloadUrl, fileName, installerType, fallbackUrl;
            if (systemInfo.OperatingSystem == OperatingSystemType.MacOS)
            {
                fileName = $"podman-{latestVersion}-macos-{architecture}.pkg";
                downloadUrl = $"https://github.com/containers/podman/releases/download/v{latestVersion}/podman-installer-macos-{architecture}.pkg";
                fallbackUrl = $"https://github.com/containers/podman/releases/download/v{latestVersion}/podman-installer-macos-{architecture}.pkg";
                installerType = "PKG";
            }
            else // Windows
            {
                fileName = $"podman-{latestVersion}-windows-{architecture}.msi";
                downloadUrl = $"https://github.com/containers/podman/releases/download/v{latestVersion}/podman-installer-windows-{architecture}.msi";
                fallbackUrl = $"https://github.com/containers/podman/releases/download/v{latestVersion}/podman-installer-windows-{architecture}.msi";
                installerType = "MSI";
            }
            
            var tempPath = Path.GetTempPath();
            var packagePath = Path.Combine(tempPath, fileName);
            
            _consoleUIService.ShowInfo($"📦 将下载Podman v{latestVersion} ({architecture})");
            _consoleUIService.ShowInfo($"🔗 首先尝试从官网下载: {downloadUrl}");
            
            bool downloadSuccess = false;
            
            // 尝试从官网下载
            try
            {
                _consoleUIService.ShowInfo("📥 正在从官网下载Podman安装包...");
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(downloadUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    var fileContent = await response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(packagePath, fileContent);
                    downloadSuccess = true;
                    _consoleUIService.ShowSuccess("✅ Podman安装包下载完成");
                }
                else
                {
                    _consoleUIService.ShowWarning($"⚠️ 官网下载失败，HTTP状态码: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "从官网下载Podman安装包时发生异常");
                _consoleUIService.ShowWarning("⚠️ 官网下载失败，尝试从GitHub下载...");
            }
            
            // 如果官网下载失败，尝试从GitHub下载
            if (!downloadSuccess)
            {
                try
                {
                    _consoleUIService.ShowInfo("📥 正在从GitHub下载Podman安装包...");
                    using var httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(fallbackUrl);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var fileContent = await response.Content.ReadAsByteArrayAsync();
                        await File.WriteAllBytesAsync(packagePath, fileContent);
                        downloadSuccess = true;
                        _consoleUIService.ShowSuccess("✅ Podman安装包下载完成");
                    }
                    else
                    {
                        _consoleUIService.ShowError($"❌ GitHub下载也失败，HTTP状态码: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "从GitHub下载Podman安装包时发生异常");
                    _consoleUIService.ShowError($"❌ GitHub下载失败: {ex.Message}");
                }
            }
            
            // 如果下载都失败了，提示用户手动下载
            if (!downloadSuccess)
            {
                _consoleUIService.ShowError("❌ 无法自动下载Podman安装包");
                _consoleUIService.ShowInfo("💡 请手动从以下地址下载并安装Podman:");
                _consoleUIService.ShowInfo($"  官网地址: https://github.com/containers/podman/releases");
                _consoleUIService.ShowInfo($"  GitHub地址: https://github.com/containers/podman/releases");
                _consoleUIService.ShowInfo("💡 安装完成后请重新运行此命令");
                return false;
            }
            
            // 安装包
            _consoleUIService.ShowInfo($"🔧 正在安装Podman {installerType}包...");
            Process process;
            
            if (systemInfo.OperatingSystem == OperatingSystemType.MacOS)
            {
                process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "installer",
                        Arguments = $"-pkg \"{packagePath}\" -target /",
                        UseShellExecute = true,
                        Verb = "runas" // 请求管理员权限
                    }
                };
            }
            else // Windows
            {
                process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "msiexec.exe",
                        Arguments = $"/i \"{packagePath}\" /quiet /norestart",
                        UseShellExecute = true,
                        Verb = "runas" // 请求管理员权限
                    }
                };
            }
            
            process.Start();
            await process.WaitForExitAsync();
            
            // 清理下载的文件
            try
            {
                File.Delete(packagePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "清理下载的安装包时发生异常: {Message}", ex.Message);
            }
            
            if (process.ExitCode == 0)
            {
                _consoleUIService.ShowSuccess("✅ Podman安装成功");
                if (systemInfo.OperatingSystem == OperatingSystemType.Windows)
                {
                    _consoleUIService.ShowInfo("💡 请重新启动终端以使环境变量生效");
                }
                return true;
            }
            else
            {
                _consoleUIService.ShowError($"❌ Podman安装失败，退出码: {process.ExitCode}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下载并安装Podman安装包时发生异常");
            _consoleUIService.ShowError($"安装过程中出现异常: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 将系统架构转换为字符串表示形式
    /// </summary>
    private string GetSystemArchitectureString(SystemArchitecture architecture)
    {
        return architecture switch
        {
            SystemArchitecture.X64 => "amd64",
            SystemArchitecture.ARM64 => RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "aarch64" : "arm64",
            SystemArchitecture.X86 => "386",
            _ => "amd64" // 默认使用amd64
        };
    }

    /// <summary>
    /// 检查命令是否可用
    /// </summary>
    private bool IsCommandAvailable(string command)
    {
        try
        {
            using var process = new Process();
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                process.StartInfo.FileName = "where";
                process.StartInfo.Arguments = command;
            }
            else
            {
                process.StartInfo.FileName = "which";
                process.StartInfo.Arguments = command;
            }

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.Start();
            process.WaitForExit();

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 初始化 Podman Machine
    /// </summary>
    private async Task InitializePodmanMachineAsync()
    {
        try
        {
            // 1. 初始化 machine
            _consoleUIService.ShowInfo("🔧 初始化 Podman Machine...");
            await ExecuteCommandAsync("podman machine init");

            // 2. 启动 machine
            _consoleUIService.ShowInfo("🚀 启动 Podman Machine...");
            await ExecuteCommandAsync("podman machine start");

            _consoleUIService.ShowSuccess("✅ Podman Machine 初始化完成");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Podman machine initialization failed");
            _consoleUIService.ShowWarning("⚠️ Podman Machine 初始化失败，可能需要手动操作");
            _consoleUIService.ShowInfo("💡 请尝试手动运行: podman machine init && podman machine start");
        }
    }

    /// <summary>
    /// 执行命令并等待完成
    /// </summary>
    private async Task<bool> ExecuteCommandAsync(string command)
    {
        try
        {
            using var process = new Process();
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = $"/c {command}";
            }
            else
            {
                process.StartInfo.FileName = "/bin/bash";
                process.StartInfo.Arguments = $"-c \"{command}\"";
            }

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.Start();
            await process.WaitForExitAsync();

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行命令失败: {Command}", command);
            return false;
        }
    }

    /// <summary>
    /// Podman安装命令信息
    /// </summary>
    private class PodmanInstallCommand
    {
        public string PackageManager { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public bool RequiresAdmin { get; set; }
        public string? WarningMessage { get; set; }
    }

    private (bool IsAvailable, List<string> MissingFiles) CheckConfigFiles(string configPath)
    {
        var missingFiles = new List<string>();
        var requiredFiles = new[] { ".env", "compose.yaml", "Dockerfile" };

        foreach (var file in requiredFiles)
        {
            var filePath = Path.Combine(configPath, file);
            if (!File.Exists(filePath))
            {
                missingFiles.Add(file);
            }
        }

        return (missingFiles.Count == 0, missingFiles);
    }

    private string GetRelativeTimeForImage(string imageDir)
    {
        try
        {
            var metadataFile = Path.Combine(imageDir, ".deck-metadata");
            if (File.Exists(metadataFile))
            {
                // TODO: 实现从元数据文件读取创建时间
                return "时间未知";
            }
            
            var directoryInfo = new DirectoryInfo(imageDir);
            var createdTime = directoryInfo.CreationTime;
            var timeSpan = DateTime.Now - createdTime;
            
            return timeSpan.TotalDays switch
            {
                < 1 => "今天",
                < 7 => $"{(int)timeSpan.TotalDays} 天前",
                < 30 => $"{(int)(timeSpan.TotalDays / 7)} 周前",
                _ => $"{(int)(timeSpan.TotalDays / 30)} 月前"
            };
        }
        catch
        {
            return "手动创建";
        }
    }

    private string GenerateUniqueConfigName(string baseName)
    {
        var configPath = Path.Combine(CustomDir, baseName);
        if (!Directory.Exists(configPath))
        {
            return baseName;
        }

        // 查找下一个可用的序号
        var maxSeq = 0;
        var pattern = $"{baseName}-";
        
        if (Directory.Exists(CustomDir))
        {
            var existingDirs = Directory.GetDirectories(CustomDir)
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrEmpty(name) && name.StartsWith(pattern));

            foreach (var dirName in existingDirs)
            {
                if (dirName != null)
                {
                    var suffix = dirName.Substring(pattern.Length);
                    if (int.TryParse(suffix, out var seq) && seq > maxSeq)
                    {
                        maxSeq = seq;
                    }
                }
            }
        }

        return $"{baseName}-{maxSeq + 1}";
    }

    private string? DetectProjectEnvironment()
    {
        var currentDir = Directory.GetCurrentDirectory();
        
        if (File.Exists(Path.Combine(currentDir, "Cargo.toml")))
            return "tauri";
        
        if (File.Exists(Path.Combine(currentDir, "pubspec.yaml")))
            return "flutter";
        
        if (Directory.GetFiles(currentDir, "*.csproj").Any())
            return "avalonia";
        
        return null;
    }

    /// <summary>
    /// 显示开发环境信息，模拟deck-shell的行为
    /// </summary>
    private void DisplayDevelopmentInfo(Dictionary<string, int> ports)
    {
        if (ports.Count == 0) return;
        
        _consoleUIService.ShowInfo("📋 开发环境信息：");
        
        if (ports.TryGetValue("DEV_PORT", out var devPort))
        {
            _consoleUIService.ShowInfo($"  🌐 开发服务：http://localhost:{devPort}");
        }
        
        if (ports.TryGetValue("DEBUG_PORT", out var debugPort))
        {
            _consoleUIService.ShowInfo($"  🐛 调试端口：{debugPort}");
        }
        
        if (ports.TryGetValue("WEB_PORT", out var webPort))
        {
            _consoleUIService.ShowInfo($"  📱 Web端口：http://localhost:{webPort}");
        }
        
        if (ports.TryGetValue("HTTPS_PORT", out var httpsPort))
        {
            _consoleUIService.ShowInfo($"  🔒 HTTPS端口：https://localhost:{httpsPort}");
        }
        
        if (ports.TryGetValue("ANDROID_DEBUG_PORT", out var androidPort))
        {
            _consoleUIService.ShowInfo($"  📱 Android调试端口：{androidPort}");
        }
    }

    private static string GetOptionDescription(StartCommandSelectableOption option)
    {
        return option.Type switch
        {
            OptionType.Image => $"启动镜像: {option.DisplayName}",
            OptionType.Config => $"从配置构建: {option.Name}",
            OptionType.Template => $"从模板创建: {option.DisplayName}",
            _ => option.DisplayName
        };
    }
}