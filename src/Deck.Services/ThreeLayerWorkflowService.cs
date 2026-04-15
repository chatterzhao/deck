using Deck.Core.Interfaces;
using Deck.Core.Models;
using Microsoft.Extensions.Logging;

namespace Deck.Services;

/// <summary>
/// 三层配置工作流程服务完整实现
/// 实现Templates、Custom、Images三层配置的完整工作流程管理
/// 注意：容器构建和启动逻辑委托给 IStartCommandService，确保唯一实现
/// </summary>
public class ThreeLayerWorkflowService : IThreeLayerWorkflowService
{
    private readonly ILogger<ThreeLayerWorkflowService> _logger;
    private readonly IDirectoryManagementService _directoryService;
    private readonly IFileSystemService _fileSystemService;
    private readonly IInteractiveSelectionService _interactiveService;
    private readonly IContainerService _containerService;
    private readonly IConfigurationService _configurationService;
    private readonly IStartCommandService _startCommandService;

    private static readonly string[] RequiredConfigFiles = { ".env", "compose.yaml", "Dockerfile" };

    public ThreeLayerWorkflowService(
        ILogger<ThreeLayerWorkflowService> logger,
        IDirectoryManagementService directoryService,
        IFileSystemService fileSystemService,
        IInteractiveSelectionService interactiveService,
        IContainerService containerService,
        IConfigurationService configurationService,
        IStartCommandService startCommandService)
    {
        _logger = logger;
        _directoryService = directoryService;
        _fileSystemService = fileSystemService;
        _interactiveService = interactiveService;
        _containerService = containerService;
        _configurationService = configurationService;
        _startCommandService = startCommandService;
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionResult> ExecuteTemplateWorkflowAsync(string templateName, string envType)
    {
        _logger.LogInformation("开始执行Templates工作流程: {TemplateName}, 环境: {EnvType}", templateName, envType);

        var result = new WorkflowExecutionResult
        {
            WorkflowType = WorkflowType.CreateEditableConfig,
            Success = false
        };

        try
        {
            // 显示双工作流程选择
            var workflowType = await _interactiveService.ShowWorkflowSelectionAsync();
            result.WorkflowType = workflowType;

            if (workflowType == WorkflowType.CreateEditableConfig)
            {
                // 工作流程1：创建可编辑配置（Templates → Custom）
                await ShowWorkflowProgressAsync(1, 1, $"将模板 {templateName} 复制到 Custom 目录");
                
                var customName = _directoryService.GenerateUniqueCustomName(templateName);
                
                if (await _directoryService.CreateCustomFromTemplateAsync(templateName, customName, envType))
                {
                    result.Success = true;
                    result.IsComplete = false; // 用户需要编辑后再运行
                    result.CustomConfigName = customName;
                    result.Messages.Add($"✅ 已创建可编辑配置: {customName}");
                    result.Messages.Add($"💡 请编辑配置后使用 'deck start {customName}' 命令启动");
                    
                    _logger.LogInformation("Templates工作流程完成 (可编辑模式): {CustomName}", customName);
                }
            }
            else
            {
                // 工作流程2：直接构建启动（Templates → Custom → Images → 构建）
                await ExecuteDirectBuildWorkflowAsync(templateName, envType, result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Templates工作流程执行失败: {TemplateName}", templateName);
            result.Errors.Add($"工作流程执行失败: {ex.Message}");
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<CustomWorkflowResult> ExecuteCustomConfigWorkflowAsync(string configName)
    {
        _logger.LogInformation("开始执行Custom配置工作流程: {ConfigName}", configName);

        var result = new CustomWorkflowResult();

        try
        {
            var customDir = _directoryService.GetCustomConfigPath(configName);
            
            // 验证配置目录存在
            if (!_fileSystemService.DirectoryExists(customDir))
            {
                result.ErrorMessage = $"配置目录不存在: {customDir}";
                _logger.LogError("配置目录不存在: {CustomDir}", customDir);
                return result;
            }

            // 验证配置完整性
            var configState = await ValidateConfigurationStateAsync(customDir);
            if (!configState.IsValid)
            {
                result.ErrorMessage = $"配置不完整，缺少文件: {string.Join(", ", configState.MissingFiles)}";
                return result;
            }

            await ShowWorkflowProgressAsync(1, 4, "验证Custom配置完整性");

            // 生成带时间戳的镜像名
            var imageName = _directoryService.GenerateTimestampedImageName(configName);
            result.ImageName = imageName;

            await ShowWorkflowProgressAsync(2, 4, $"创建Images目录: {imageName}");

            // Custom → Images：复制配置到Images目录
            var imageDir = await _directoryService.CreateImageFromCustomAsync(imageName, customDir);
            
            await ShowWorkflowProgressAsync(3, 4, "开始容器镜像构建");

            // 执行镜像构建和容器启动
            var buildResult = await BuildAndStartContainerAsync(imageName, imageDir);
            
            await ShowWorkflowProgressAsync(4, 4, buildResult.Success ? "容器启动成功" : "容器启动失败");

            result.Success = buildResult.Success;
            result.ContainerName = buildResult.ContainerName;
            result.Messages.AddRange(buildResult.Messages);
            
            if (!buildResult.Success)
            {
                result.ErrorMessage = buildResult.ErrorMessage;
            }

            // 更新镜像元数据
            var metadata = new ImageMetadata
            {
                ImageName = imageName,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Environment.UserName,
                SourceConfig = customDir,
                BuildStatus = buildResult.Success ? BuildStatus.Built : BuildStatus.Failed,
                LastStarted = buildResult.Success ? DateTime.UtcNow : null
            };
            await UpdateImageMetadataAsync(imageDir, metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Custom配置工作流程执行失败: {ConfigName}", configName);
            result.ErrorMessage = $"工作流程执行失败: {ex.Message}";
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<ImagesWorkflowResult> ExecuteImagesWorkflowAsync(string imageName)
    {
        _logger.LogInformation("开始执行Images工作流程: {ImageName}", imageName);

        var result = new ImagesWorkflowResult
        {
            ImageName = imageName,
            Success = false,
            Action = ContainerAction.BuildAndStart,
            ContainerName = $"deck_{imageName}"
        };

        try
        {
            var imageDir = _directoryService.GetImageDirectory(imageName);
            
            if (!_fileSystemService.DirectoryExists(imageDir))
            {
                result.Messages.Add($"❌ 镜像目录不存在: {imageDir}");
                return result;
            }

            // 检查容器状态，决定执行的操作
            var containerName = $"deck_{imageName}";
            var containerStatus = await _containerService.DetectContainerStatusAsync(containerName);
            
            switch (containerStatus.Status)
            {
                case ContainerStatus.Running:
                    result.Action = ContainerAction.Enter;
                    result.Success = true;
                    result.Messages.Add($"✅ 容器 {containerName} 已在运行");
                    break;
                    
                case ContainerStatus.Stopped:
                    result.Action = ContainerAction.Restart;
                    var startResult = await _containerService.StartContainerAsync(containerName);
                    result.Success = startResult.Success;
                    result.Messages.Add(startResult.Success ? 
                        $"✅ 已启动现有容器: {containerName}" : 
                        $"❌ 启动容器失败: {startResult.Message}");
                    break;
                    
                case ContainerStatus.NotExists:
                default:
                    result.Action = ContainerAction.BuildAndStart;
                    var buildResult = await BuildAndStartContainerAsync(imageName, imageDir);
                    result.Success = buildResult.Success;
                    result.Messages.AddRange(buildResult.Messages);
                    break;
            }

            // 更新镜像元数据
            if (result.Success)
            {
                var metadata = await ReadImageMetadataAsync(imageDir);
                if (metadata != null)
                {
                    metadata.LastStarted = DateTime.UtcNow;
                    metadata.BuildStatus = BuildStatus.Running;
                    await UpdateImageMetadataAsync(imageDir, metadata);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Images工作流程执行失败: {ImageName}", imageName);
            result.Messages.Add($"❌ 工作流程执行失败: {ex.Message}");
        }

        return result;
    }

    /// <inheritdoc />
    public Task<ConfigurationStateResult> ValidateConfigurationStateAsync(string configPath)
    {
        var result = new ConfigurationStateResult();

        try
        {
            if (!_fileSystemService.DirectoryExists(configPath))
            {
                result.Status = ConfigValidationStatus.NotFound;
                result.ErrorMessage = $"配置目录不存在: {configPath}";
                return Task.FromResult(result);
            }

            var missingFiles = new List<string>();
            
            foreach (var requiredFile in RequiredConfigFiles)
            {
                var filePath = Path.Combine(configPath, requiredFile);
                if (!_fileSystemService.FileExists(filePath))
                {
                    missingFiles.Add(requiredFile);
                }
            }

            result.MissingFiles = missingFiles;
            result.IsValid = missingFiles.Count == 0;
            result.Status = result.IsValid ? ConfigValidationStatus.Complete : ConfigValidationStatus.Incomplete;

            _logger.LogDebug("配置验证结果: {ConfigPath}, 有效: {IsValid}, 缺少文件: {MissingFiles}", 
                configPath, result.IsValid, string.Join(", ", missingFiles));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "配置验证失败: {ConfigPath}", configPath);
            result.Status = ConfigValidationStatus.Invalid;
            result.ErrorMessage = $"配置验证失败: {ex.Message}";
        }

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public async Task UpdateImageMetadataAsync(string imageDir, ImageMetadata metadata)
    {
        try
        {
            var metadataFile = Path.Combine(imageDir, ".deck-metadata");
            var lines = new[]
            {
                $"IMAGE_NAME={metadata.ImageName}",
                $"CREATED_AT={metadata.CreatedAt:yyyy-MM-ddTHH:mm:ssZ}",
                $"CREATED_BY={metadata.CreatedBy}",
                $"SOURCE_CONFIG={metadata.SourceConfig}",
                $"BUILD_STATUS={metadata.BuildStatus}",
                $"LAST_STARTED={metadata.LastStarted?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? ""}"
            };
            
            await _fileSystemService.WriteTextFileAsync(metadataFile, string.Join(Environment.NewLine, lines));
            _logger.LogDebug("已更新镜像元数据: {ImageDir}", imageDir);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新镜像元数据失败: {ImageDir}", imageDir);
        }
    }

    /// <inheritdoc />
    public async Task<ImageMetadata?> ReadImageMetadataAsync(string imageDir)
    {
        try
        {
            var metadataFile = Path.Combine(imageDir, ".deck-metadata");
            if (!_fileSystemService.FileExists(metadataFile))
            {
                return null;
            }

            var content = await _fileSystemService.ReadTextFileAsync(metadataFile);
            if (content == null) return null;
            
            var lines = content.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            
            var metadata = new ImageMetadata { ImageName = "" };
            
            foreach (var line in lines)
            {
                var parts = line.Split('=', 2);
                if (parts.Length != 2) continue;
                
                var key = parts[0].Trim();
                var value = parts[1].Trim();
                
                switch (key)
                {
                    case "IMAGE_NAME":
                        metadata.ImageName = value;
                        break;
                    case "CREATED_AT":
                        if (DateTime.TryParse(value, out var createdAt))
                            metadata.CreatedAt = createdAt;
                        break;
                    case "CREATED_BY":
                        metadata.CreatedBy = value;
                        break;
                    case "SOURCE_CONFIG":
                        metadata.SourceConfig = value;
                        break;
                    case "BUILD_STATUS":
                        if (Enum.TryParse<BuildStatus>(value, out var buildStatus))
                            metadata.BuildStatus = buildStatus;
                        break;
                    case "LAST_STARTED":
                        if (DateTime.TryParse(value, out var lastStarted))
                            metadata.LastStarted = lastStarted;
                        break;
                }
            }

            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取镜像元数据失败: {ImageDir}", imageDir);
            return null;
        }
    }

    /// <inheritdoc />
    public Task<List<string>> GenerateConfigurationChainAsync(string? templateName = null, string? customName = null, string? imageName = null)
    {
        var chain = new List<string>();

        try
        {
            if (!string.IsNullOrEmpty(templateName))
            {
                chain.Add($"📋 Templates: {templateName}");
            }

            if (!string.IsNullOrEmpty(customName))
            {
                chain.Add($"🔧 Custom: {customName}");
            }

            if (!string.IsNullOrEmpty(imageName))
            {
                chain.Add($"📦 Images: {imageName}");
            }

            if (chain.Count > 1)
            {
                // 添加箭头连接
                for (int i = 0; i < chain.Count - 1; i++)
                {
                    chain[i] += " → ";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成配置链路失败");
            chain.Add("⚠️  配置链路生成失败");
        }

        return Task.FromResult(chain);
    }

    /// <inheritdoc />
    public async Task ShowWorkflowProgressAsync(int currentStep, int totalSteps, string stepDescription)
    {
        var progressMessage = $"[步骤 {currentStep}/{totalSteps}] {stepDescription}";
        _logger.LogInformation("工作流程进度: {ProgressMessage}", progressMessage);
        
        Console.WriteLine($"🚀 {progressMessage}");
        
        // 添加短暂延时以提供更好的用户体验
        await Task.Delay(500);
    }

    #region Private Methods

    /// <summary>
    /// 执行直接构建工作流程
    /// Templates → Custom → Images → 构建
    /// </summary>
    private async Task ExecuteDirectBuildWorkflowAsync(string templateName, string envType, WorkflowExecutionResult result)
    {
        await ShowWorkflowProgressAsync(1, 4, $"步骤 1/4: 从模板 {templateName} 创建临时配置");
        
        // Step 1: Templates → Custom
        var customName = _directoryService.GenerateTimestampedName($"{templateName}-temp");
        if (!await _directoryService.CreateCustomFromTemplateAsync(templateName, customName, envType))
        {
            result.Errors.Add("创建临时配置失败");
            return;
        }

        await ShowWorkflowProgressAsync(2, 4, $"步骤 2/4: 创建镜像目录");
        
        // Step 2: Custom → Images  
        var imageName = _directoryService.GenerateImageName(customName);
        var customDir = _directoryService.GetCustomConfigPath(customName);
        var imageDir = await _directoryService.CreateImageFromCustomAsync(imageName, customDir);
        
        await ShowWorkflowProgressAsync(3, 4, $"步骤 3/4: 构建容器镜像");
        
        // Step 3: 执行镜像构建和容器启动
        var buildResult = await BuildAndStartContainerAsync(imageName, imageDir);
        
        await ShowWorkflowProgressAsync(4, 4, buildResult.Success ? "步骤 4/4: 容器启动成功" : "步骤 4/4: 容器启动失败");
        
        // Step 4: 设置结果
        result.Success = buildResult.Success;
        result.IsComplete = buildResult.Success;
        result.ImageName = imageName;
        result.CustomConfigName = customName;
        result.Messages.AddRange(buildResult.Messages);
        
        if (!buildResult.Success)
        {
            result.Errors.Add(buildResult.ErrorMessage ?? "构建过程失败");
        }
        
        // 更新元数据
        var metadata = new ImageMetadata
        {
            ImageName = imageName,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Environment.UserName,
            SourceConfig = customDir,
            BuildStatus = buildResult.Success ? BuildStatus.Built : BuildStatus.Failed,
            LastStarted = buildResult.Success ? DateTime.UtcNow : null
        };
        await UpdateImageMetadataAsync(imageDir, metadata);
    }

    /// <summary>
    /// 构建并启动容器
    /// 委托给 IStartCommandService.StartFromImageAsync 以确保使用 compose up -d --build
    /// </summary>
    private async Task<ContainerBuildResult> BuildAndStartContainerAsync(string imageName, string imageDir)
    {
        var result = new ContainerBuildResult
        {
            ContainerName = imageName,
            Messages = new List<string>()
        };

        try
        {
            // 验证必要的配置文件存在
            var composeFile = Path.Combine(imageDir, "compose.yaml");
            if (!_fileSystemService.FileExists(composeFile))
            {
                result.ErrorMessage = $"缺少 compose.yaml 文件: {composeFile}";
                result.Messages.Add($"❌ {result.ErrorMessage}");
                return result;
            }

            _logger.LogInformation("开始构建容器镜像: {ImageName}", imageName);
            result.Messages.Add($"🔨 开始构建镜像: {imageName}");

            // 委托给 StartCommandService，它使用 compose up -d --build
            var startResult = await _startCommandService.StartFromImageAsync(imageName);
            
            result.Success = startResult.IsSuccess;
            result.ContainerName = startResult.ContainerName ?? imageName;
            result.Messages.Add(startResult.IsSuccess ? 
                $"✅ 容器启动成功: {result.ContainerName}" : 
                $"❌ 容器启动失败: {startResult.ErrorMessage}");

            if (!startResult.IsSuccess)
            {
                result.ErrorMessage = startResult.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "构建容器失败: {ImageName}", imageName);
            result.ErrorMessage = $"构建失败: {ex.Message}";
            result.Messages.Add($"❌ {result.ErrorMessage}");
        }

        return result;
    }

    #endregion

    /// <summary>
    /// 容器构建结果内部类
    /// </summary>
    private class ContainerBuildResult
    {
        public bool Success { get; set; }
        public string ContainerName { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public List<string> Messages { get; set; } = new();
    }
}