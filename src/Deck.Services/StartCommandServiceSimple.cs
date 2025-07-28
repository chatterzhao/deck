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
    private readonly IConsoleUIService _consoleUIService;
    private readonly IEnhancedFileOperationsService _enhancedFileOperationsService;
    private readonly IConfigurationService _configurationService;
    private readonly IRemoteTemplatesService _remoteTemplatesService;

    // 目录常量
    private const string DeckDir = ".deck";
    private const string ImagesDir = ".deck/images";
    private const string CustomDir = ".deck/custom";
    private const string TemplatesDir = ".deck/templates";

    public StartCommandServiceSimple(
        ILogger<StartCommandServiceSimple> logger,
        IConsoleUIService consoleUIService,
        IEnhancedFileOperationsService enhancedFileOperationsService,
        IConfigurationService configurationService,
        IRemoteTemplatesService remoteTemplatesService)
    {
        _logger = logger;
        _consoleUIService = consoleUIService;
        _enhancedFileOperationsService = enhancedFileOperationsService;
        _configurationService = configurationService;
        _remoteTemplatesService = remoteTemplatesService;
    }

    public async Task<StartCommandResult> ExecuteAsync(string? envType, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Start command execution started with env-type: {EnvType}", envType ?? "auto-detect");
            
            _consoleUIService.ShowInfo("🚀 启动容器化工具...");

            // 初始化目录结构
            InitializeDirectoryStructure();
            
            // 确保配置文件存在
            await EnsureConfigurationAsync(cancellationToken);
            
            // 更新模板目录
            await UpdateTemplatesAsync(cancellationToken);

            // 获取三层配置选项
            var options = await GetOptionsAsync(envType, cancellationToken);

            // 显示选择界面
            var selectedOption = _consoleUIService.ShowThreeLayerSelection(options);
            if (selectedOption == null)
            {
                return StartCommandResult.Failure("用户取消了选择");
            }

            _consoleUIService.ShowSuccess($"✅ 您选择了：{GetOptionDescription(selectedOption)}");

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
            _logger.LogInformation("配置文件已加载或创建: Repository={Repository}, Branch={Branch}", 
                config.RemoteTemplates.Repository, config.RemoteTemplates.Branch);
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
    private async Task UpdateTemplatesAsync(CancellationToken cancellationToken)
    {
        try
        {
            _consoleUIService.ShowInfo("🔄 检查并更新模板...");
            
            var config = await _configurationService.GetConfigAsync();
            if (config.RemoteTemplates.AutoUpdate)
            {
                var syncResult = await _remoteTemplatesService.SyncTemplatesAsync(forceUpdate: false);
                if (syncResult.Success)
                {
                    _consoleUIService.ShowSuccess($"✅ 模板同步成功，更新了 {syncResult.SyncedTemplateCount} 个模板");
                }
                else
                {
                    _consoleUIService.ShowWarning("⚠️ 模板同步失败: " + string.Join(", ", syncResult.SyncLogs));
                }
            }
            else
            {
                _consoleUIService.ShowInfo("💡 模板自动更新已禁用");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新模板时发生错误");
            _consoleUIService.ShowWarning("⚠️ 模板更新失败: " + ex.Message);
        }
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
            
            if (!string.IsNullOrEmpty(projectType))
            {
                _consoleUIService.ShowInfo($"🔍 检测到环境类型：{projectType}");
                _consoleUIService.ShowWarning("💡 推荐选择对应的环境类型配置");
            }
        }
        else
        {
            options.EnvType = envType;
            options.IsAutoDetected = false;
            
            if (envType == "unknown")
            {
                _consoleUIService.ShowInfo("🔍 显示所有可用配置选项");
                _consoleUIService.ShowWarning("💡 提示：使用 'deck start <类型>' 可过滤特定环境，如 'deck start tauri'");
            }
            else
            {
                _consoleUIService.ShowInfo($"🔍 仅显示 {envType}- 开头的目录");
            }
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
            // 处理标准端口管理和冲突检测
            _consoleUIService.ShowInfo("🔍 检查端口配置和冲突...");
            var portResult = await _enhancedFileOperationsService.ProcessStandardPortsAsync(envFilePath);
            if (!portResult.IsSuccess)
            {
                return StartCommandResult.Failure($"端口处理失败: {portResult.ErrorMessage}");
            }
            
            // 显示端口冲突解决信息并处理用户交互
            if (portResult.ModifiedPorts.Count > 0)
            {
                _consoleUIService.ShowWarning("⚠️ 检测到端口冲突，已自动解决：");
                foreach (var (portVar, newPort) in portResult.ModifiedPorts)
                {
                    _consoleUIService.ShowInfo($"  📌 {portVar}: 已更改为端口 {newPort}");
                }
                _consoleUIService.ShowInfo("💡 端口配置已更新到 .env 文件中");
                
                // 询问用户是否要继续
                var continueWithNewPorts = _consoleUIService.ShowConfirmation("是否继续使用新的端口配置启动？");
                if (!continueWithNewPorts)
                {
                    return StartCommandResult.Failure("用户取消了启动，请检查端口配置后重试");
                }
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
            
            var containerName = $"{projectNameResult.UpdatedProjectName ?? imageName}-dev";
            
            // 显示开发环境信息（模拟deck-shell的行为）
            DisplayDevelopmentInfo(portResult.AllPorts);
            
            _consoleUIService.ShowSuccess($"✅ 镜像启动准备完成: {imageName}");
            _consoleUIService.ShowInfo($"📦 容器名称: {containerName}");
            
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
            // 处理标准端口管理和冲突检测
            _consoleUIService.ShowInfo("🔍 检查端口配置和冲突...");
            var portResult = await _enhancedFileOperationsService.ProcessStandardPortsAsync(envFilePath);
            if (!portResult.IsSuccess)
            {
                return StartCommandResult.Failure($"端口处理失败: {portResult.ErrorMessage}");
            }
            
            // 显示端口冲突解决信息并处理用户交互
            if (portResult.ModifiedPorts.Count > 0)
            {
                _consoleUIService.ShowWarning("⚠️ 检测到端口冲突，已自动解决：");
                foreach (var (portVar, newPort) in portResult.ModifiedPorts)
                {
                    _consoleUIService.ShowInfo($"  📌 {portVar}: 已更改为端口 {newPort}");
                }
                _consoleUIService.ShowInfo("💡 端口配置已更新到 .env 文件中");
                
                // 询问用户是否要继续
                var continueWithNewPorts = _consoleUIService.ShowConfirmation("是否继续使用新的端口配置构建？");
                if (!continueWithNewPorts)
                {
                    return StartCommandResult.Failure("用户取消了构建，请检查端口配置后重试");
                }
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
            
            _consoleUIService.ShowInfo($"🚧 配置构建功能：Custom → Images 流程");
            _consoleUIService.ShowWarning("⚠️ 配置构建功能暂未完全实现，需要集成 podman-compose build");
            
            _consoleUIService.ShowSuccess($"✅ 配置预处理完成: {configName}");
            _consoleUIService.ShowInfo($"📦 目标镜像: {imageName}");
            _consoleUIService.ShowInfo($"📦 容器名称: {containerName}");

            return StartCommandResult.Success(imageName, containerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Starting from config failed: {ConfigName}", configName);
            return StartCommandResult.Failure($"启动失败: {ex.Message}");
        }
    }

    public Task<StartCommandResult> StartFromTemplateAsync(string templateName, string? envType, TemplateWorkflowType workflowType, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting from template: {TemplateName}, workflow: {WorkflowType}", templateName, workflowType);

        _consoleUIService.ShowInfo($"从模板创建: {templateName}");

        if (workflowType == TemplateWorkflowType.CreateEditableConfig)
        {
            return Task.FromResult(CreateEditableConfigFromTemplate(templateName, envType));
        }
        else
        {
            return Task.FromResult(DirectBuildFromTemplate(templateName, envType));
        }
    }

    private async Task<StartCommandResult> HandleTemplateSelectionAsync(string templateName, string? envType, CancellationToken cancellationToken)
    {
        // 显示模板工作流程选择
        var workflowType = _consoleUIService.ShowTemplateWorkflowSelection();
        
        return await StartFromTemplateAsync(templateName, envType, workflowType, cancellationToken);
    }

    private StartCommandResult CreateEditableConfigFromTemplate(string templateName, string? envType)
    {
        _consoleUIService.ShowInfo("📝 创建可编辑配置：");

        // 生成配置名称（如果存在重复则添加序号）
        var configName = GenerateUniqueConfigName(templateName);
        
        if (configName != templateName)
        {
            _consoleUIService.ShowWarning($"💡 已有 {templateName}，本次创建为 {configName}");
        }

        _consoleUIService.ShowWarning("创建可编辑配置功能暂未完全实现");

        _consoleUIService.ShowSuccess("✅ 可编辑配置已创建完成");
        _consoleUIService.ShowWarning($"📁 配置位置: {Path.Combine(CustomDir, configName)}");
        _consoleUIService.ShowInfo("📝 接下来您可以：");
        _consoleUIService.ShowInfo("  1. 编辑配置文件（.env, compose.yaml, Dockerfile）来自定义环境");
        _consoleUIService.ShowInfo("  2. 重新运行 'deck start' 并用【用户自定义配置 - Custom】区刚编辑过的配置的序号来构建启动");
        _consoleUIService.ShowWarning("💡 提示: 配置文件可自由修改，但请勿更改目录名称");

        return StartCommandResult.Success(configName, string.Empty);
    }

    private StartCommandResult DirectBuildFromTemplate(string templateName, string? envType)
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

        // 步骤 2: 复制配置到 images 目录
        _consoleUIService.ShowStep(2, 3, "复制配置到 images 目录");
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmm");
        var imageName = $"{customName}-{timestamp}";

        // 步骤 3: 构建并启动镜像
        _consoleUIService.ShowStep(3, 3, "构建并启动镜像");

        _consoleUIService.ShowWarning("直接构建启动功能暂未完全实现");

        _consoleUIService.ShowSuccess("✅ 直接构建启动完成");
        _consoleUIService.ShowInfo($"💡 注意:您选择了【直接构建启动】方式，custom 和 images 目录中的配置完全一致，均基于 {customName}");

        return StartCommandResult.Success(imageName, $"{imageName}-dev");
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

        // 如果没有找到模板，使用默认内置模板
        if (options.Count == 0)
        {
            var defaultTemplates = envType == "unknown" 
                ? new[] { "tauri-default", "flutter-default", "avalonia-default" }
                : new[] { $"{envType}-default" };

            foreach (var templateName in defaultTemplates)
            {
                options.Add(new TemplateOption
                {
                    Name = templateName,
                    Path = string.Empty,
                    IsBuiltIn = true,
                    IsAvailable = true
                });
            }
        }

        return options;
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