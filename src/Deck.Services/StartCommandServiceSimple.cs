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
    private readonly IEnvironmentConfigurationService _environmentConfigurationService;
    private readonly IContainerEngineManagementService _containerEngineManagementService; // 添加容器引擎管理服务

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
        IFileSystemService fileSystemService,
        IEnvironmentConfigurationService environmentConfigurationService,
        IContainerEngineManagementService containerEngineManagementService) // 注入容器引擎管理服务
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _consoleUIService = consoleUIService;
        _enhancedFileOperationsService = enhancedFileOperationsService;
        _configurationService = configurationService;
        _remoteTemplatesService = remoteTemplatesService;
        _fileSystemService = fileSystemService;
        _environmentConfigurationService = environmentConfigurationService;
        _containerEngineManagementService = containerEngineManagementService; // 初始化容器引擎管理服务
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
            var containerEngineInfo = await _containerEngineManagementService.CheckAndHandleContainerEngineAsync();
            
            // 检查是否需要重新安装（例如brew安装的情况）
            if (!await _containerEngineManagementService.CheckAndHandlePodmanReinstallationAsync())
            {
                return StartCommandResult.Failure("Podman重新安装失败");
            }
            
            // 重新检测容器引擎
            containerEngineInfo = await _containerEngineManagementService.CheckAndHandleContainerEngineAsync();
            
            if (containerEngineInfo.Type == ContainerEngineType.None)
            {
                // 尝试安装容器引擎
                var installResult = await _containerEngineManagementService.InstallContainerEngineAsync();
                if (!installResult)
                {
                    return StartCommandResult.Failure("未检测到可用的容器引擎，且自动安装失败");
                }
                
                // 重新检测
                containerEngineInfo = await _containerEngineManagementService.CheckAndHandleContainerEngineAsync();
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
            
            // 对于Images分支，需要根据镜像名称确定环境类型，然后构造正确的容器名称
            var environment = DetermineEnvironmentFromImageName(imageName);
            var baseName = projectNameResult.UpdatedProjectName ?? imageName;
            var containerName = EnvironmentHelper.GetContainerName(baseName, environment);
            
            // 显示开发环境信息（模拟deck-shell的行为）
            DisplayDevelopmentInfo(portResult.AllPorts, environment);
            
            // 对于已构建镜像，使用docker-compose启动（检查容器是否存在，不存在则构建并启动）
            var startSuccess = await StartContainerAsync(imageName, containerName, engineName.ToLower(), cancellationToken);
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
            var containerEngineInfo = await _containerEngineManagementService.CheckAndHandleContainerEngineAsync();
            
            // 检查是否需要重新安装（例如brew安装的情况）
            if (!await _containerEngineManagementService.CheckAndHandlePodmanReinstallationAsync())
            {
                return StartCommandResult.Failure("Podman重新安装失败");
            }
            
            // 重新检测容器引擎
            containerEngineInfo = await _containerEngineManagementService.CheckAndHandleContainerEngineAsync();
            
            if (containerEngineInfo.Type == ContainerEngineType.None)
            {
                // 尝试安装容器引擎
                var installResult = await _containerEngineManagementService.InstallContainerEngineAsync();
                if (!installResult)
                {
                    return StartCommandResult.Failure("未检测到可用的容器引擎，且自动安装失败");
                }
                
                // 重新检测
                containerEngineInfo = await _containerEngineManagementService.CheckAndHandleContainerEngineAsync();
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

            // 显示环境选择
            var environment = _consoleUIService.ShowEnvironmentSelection();
            if (environment == null)
            {
                return StartCommandResult.Failure("用户取消了环境选择");
            }

            // 生成镜像名称（配置名称 + 时间戳 + 环境后缀）
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmm");
            var envSuffix = EnvironmentHelper.GetEnvironmentOption(environment.Value).ContainerSuffix;
            var imageName = $"{configName}-{timestamp}-{envSuffix}";
            
            // 更新 PROJECT_NAME 避免容器名冲突
            _consoleUIService.ShowInfo("🏷️ 更新项目名称...");
            var projectNameResult = await _enhancedFileOperationsService.UpdateProjectNameAsync(envFilePath, imageName);
            if (!projectNameResult.IsSuccess)
            {
                _logger.LogWarning("PROJECT_NAME更新失败: {Error}", projectNameResult.ErrorMessage);
            }
            
            var containerName = EnvironmentHelper.GetContainerName(projectNameResult.UpdatedProjectName ?? imageName, environment.Value);
            
            // 显示开发环境信息
            DisplayDevelopmentInfo(portResult.AllPorts, environment.Value);
            
            // 实际执行构建和启动流程
            var buildResult = await BuildAndStartContainer(configName, imageName, containerName, environment.Value, engineName.ToLower(), cancellationToken);
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
            var containerEngineInfo = await _containerEngineManagementService.CheckAndHandleContainerEngineAsync();
            
            // 检查是否需要重新安装（例如brew安装的情况）
            if (!await _containerEngineManagementService.CheckAndHandlePodmanReinstallationAsync())
            {
                return StartCommandResult.Failure("Podman重新安装失败");
            }
            
            // 重新检测容器引擎
            containerEngineInfo = await _containerEngineManagementService.CheckAndHandleContainerEngineAsync();
            
            if (containerEngineInfo.Type == ContainerEngineType.None)
            {
                // 尝试安装容器引擎
                var installResult = await _containerEngineManagementService.InstallContainerEngineAsync();
                if (!installResult)
                {
                    return StartCommandResult.Failure("未检测到可用的容器引擎，且自动安装失败");
                }
                
                // 重新检测
                containerEngineInfo = await _containerEngineManagementService.CheckAndHandleContainerEngineAsync();
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

        // 显示环境选择
        var environment = _consoleUIService.ShowEnvironmentSelection();
        if (environment == null)
        {
            return StartCommandResult.Failure("用户取消了环境选择");
        }

        // 步骤 2: 复制配置到 images 目录
        _consoleUIService.ShowStep(2, 3, "复制配置到 images 目录");
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmm");
        var envSuffix = EnvironmentHelper.GetEnvironmentOption(environment.Value).ContainerSuffix;
        var imageName = $"{customName}-{timestamp}-{envSuffix}";

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

        // 更新环境配置文件
        _consoleUIService.ShowInfo("⚙️ 更新环境配置...");
        var composeFilePath = Path.Combine(targetPath, "compose.yaml");
        var envFilePath = Path.Combine(targetPath, ".env");
        
        await _environmentConfigurationService.UpdateComposeEnvironmentAsync(composeFilePath, environment.Value, imageName);
        await _environmentConfigurationService.UpdateEnvFileEnvironmentAsync(envFilePath, environment.Value);

        // 处理端口冲突和项目名称
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
            DisplayDevelopmentInfo(portResult.AllPorts, environment.Value);
        }

        // 步骤 3: 构建并启动镜像
        _consoleUIService.ShowStep(3, 3, "构建并启动镜像");

        var containerName = EnvironmentHelper.GetContainerName(imageName, environment.Value);

        // 3. 构建并启动容器（使用docker-compose一步完成）
        _consoleUIService.ShowInfo("🔨 正在构建并启动容器...");
        var startSuccess = await StartContainerAsync(imageName, containerName, engine, cancellationToken);
        if (!startSuccess)
        {
            return StartCommandResult.Failure("容器构建或启动失败");
        }
        _consoleUIService.ShowSuccess($"✅ 容器构建并启动成功: {containerName}");

        return StartCommandResult.Success(imageName, containerName);
    }

    /// <summary>
    /// 构建镜像并启动容器的实际实现
    /// </summary>
    private async Task<StartCommandResult> BuildAndStartContainer(string configName, string imageName, string containerName, EnvironmentType environment, string engine, CancellationToken cancellationToken)
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

            // 更新环境配置文件
            _consoleUIService.ShowInfo("⚙️ 更新环境配置...");
            var composeFilePath = Path.Combine(targetPath, "compose.yaml");
            var envFilePath = Path.Combine(targetPath, ".env");
            
            await _environmentConfigurationService.UpdateComposeEnvironmentAsync(composeFilePath, environment, imageName);
            await _environmentConfigurationService.UpdateEnvFileEnvironmentAsync(envFilePath, environment);

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
            // 在执行容器命令前，再次检查容器引擎是否可用
            _consoleUIService.ShowInfo("🔍 再次检查容器引擎...");
            var containerEngineInfo = await _containerEngineManagementService.CheckAndHandleContainerEngineAsync();
            
            // 检查是否需要重新安装（例如brew安装的情况）
            if (!await _containerEngineManagementService.CheckAndHandlePodmanReinstallationAsync())
            {
                _consoleUIService.ShowError("❌ Podman重新安装失败");
                return false;
            }
            
            // 重新检测容器引擎
            containerEngineInfo = await _containerEngineManagementService.CheckAndHandleContainerEngineAsync();
            
            if (containerEngineInfo.Type == ContainerEngineType.None || !containerEngineInfo.IsAvailable)
            {
                // 尝试安装容器引擎
                var installResult = await _containerEngineManagementService.InstallContainerEngineAsync();
                if (!installResult)
                {
                    _consoleUIService.ShowError("❌ 未检测到可用的容器引擎，且自动安装失败");
                    return false;
                }
                
                // 重新检测
                containerEngineInfo = await _containerEngineManagementService.CheckAndHandleContainerEngineAsync();
                if (containerEngineInfo.Type == ContainerEngineType.None || !containerEngineInfo.IsAvailable)
                {
                    _consoleUIService.ShowError("❌ 容器引擎安装后仍无法检测到，请手动检查");
                    return false;
                }
            }
            
            // 确保使用正确的引擎名称
            var engineName = containerEngineInfo.Type == ContainerEngineType.Podman ? "podman" : "docker";
            _consoleUIService.ShowInfo($"🚀 使用容器引擎: {engineName}");

            // 使用compose文件构建并启动容器
            var startInfo = new ProcessStartInfo
            {
                FileName = engineName,
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
            _consoleUIService.ShowInfo($"🚀 启动现有容器: {containerName}");
            
            using var process = new Process();
            process.StartInfo.FileName = engine;
            process.StartInfo.Arguments = $"start {containerName}";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.Start();
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0)
            {
                _consoleUIService.ShowSuccess($"✅ 容器已启动: {containerName}");
                return true;
            }
            else
            {
                var error = await process.StandardError.ReadToEndAsync();
                _consoleUIService.ShowError($"❌ 启动容器失败: {error}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动容器时发生异常: {ContainerName}", containerName);
            _consoleUIService.ShowError($"启动容器时发生异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Podman安装命令信息
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
    /// 根据镜像名称确定环境类型
    /// 镜像名称格式: {baseName}-{timestamp}-{envSuffix}
    /// </summary>
    private EnvironmentType DetermineEnvironmentFromImageName(string imageName)
    {
        if (imageName.EndsWith("-dev"))
            return EnvironmentType.Development;
        if (imageName.EndsWith("-test"))
            return EnvironmentType.Test;
        if (imageName.EndsWith("-prod"))
            return EnvironmentType.Production;
        
        // 默认为开发环境
        return EnvironmentType.Development;
    }

    /// <summary>
    /// 显示开发环境信息，模拟deck-shell的行为（Image分支使用，默认开发环境）
    /// </summary>
    private void DisplayDevelopmentInfo(Dictionary<string, int> ports)
    {
        DisplayDevelopmentInfo(ports, EnvironmentType.Development);
    }

    /// <summary>
    /// 显示开发环境信息，模拟deck-shell的行为
    /// </summary>
    private void DisplayDevelopmentInfo(Dictionary<string, int> ports, EnvironmentType environment)
    {
        if (ports.Count == 0) return;
        
        var envOption = EnvironmentHelper.GetEnvironmentOption(environment);
        _consoleUIService.ShowInfo($"📋 {envOption.DisplayName}信息：");
        
        if (ports.TryGetValue("DEV_PORT", out var devPort))
        {
            var adjustedPort = EnvironmentHelper.CalculatePort(devPort, environment);
            _consoleUIService.ShowInfo($"  🌐 开发服务：http://localhost:{adjustedPort}");
        }
        
        if (ports.TryGetValue("DEBUG_PORT", out var debugPort))
        {
            var adjustedPort = EnvironmentHelper.CalculatePort(debugPort, environment);
            _consoleUIService.ShowInfo($"  🐛 调试端口：{adjustedPort}");
        }
        
        if (ports.TryGetValue("WEB_PORT", out var webPort))
        {
            var adjustedPort = EnvironmentHelper.CalculatePort(webPort, environment);
            _consoleUIService.ShowInfo($"  📱 Web端口：http://localhost:{adjustedPort}");
        }
        
        if (ports.TryGetValue("HTTPS_PORT", out var httpsPort))
        {
            var adjustedPort = EnvironmentHelper.CalculatePort(httpsPort, environment);
            _consoleUIService.ShowInfo($"  🔒 HTTPS端口：https://localhost:{adjustedPort}");
        }
        
        if (ports.TryGetValue("ANDROID_DEBUG_PORT", out var androidPort))
        {
            var adjustedPort = EnvironmentHelper.CalculatePort(androidPort, environment);
            _consoleUIService.ShowInfo($"  📱 Android调试端口：{adjustedPort}");
        }

        // 显示环境特定的警告信息
        if (environment == EnvironmentType.Production)
        {
            _consoleUIService.ShowWarning("⚠️ 生产环境已启动，请确保配置正确");
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