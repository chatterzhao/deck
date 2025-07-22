using Deck.Core.Interfaces;
using Deck.Core.Models;
using Microsoft.Extensions.Logging;

namespace Deck.Services;

/// <summary>
/// 三层配置工作流程服务桩实现 - 用于测试和初期开发
/// 完整实现待ContainerService完善后替换
/// </summary>
public class ThreeLayerWorkflowServiceStub : IThreeLayerWorkflowService
{
    private readonly ILogger<ThreeLayerWorkflowServiceStub> _logger;
    private readonly IDirectoryManagementService _directoryService;
    private readonly IFileSystemService _fileSystemService;
    private readonly IInteractiveSelectionService _interactiveService;

    private static readonly string[] RequiredConfigFiles = { ".env", "compose.yaml", "Dockerfile" };

    public ThreeLayerWorkflowServiceStub(
        ILogger<ThreeLayerWorkflowServiceStub> logger,
        IDirectoryManagementService directoryService,
        IFileSystemService fileSystemService,
        IInteractiveSelectionService interactiveService)
    {
        _logger = logger;
        _directoryService = directoryService;
        _fileSystemService = fileSystemService;
        _interactiveService = interactiveService;
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

            await ShowWorkflowProgressAsync(1, 2, "验证Custom配置完整性");

            // 生成带时间戳的镜像名
            var imageName = _directoryService.GenerateTimestampedImageName(configName);
            result.ImageName = imageName;

            await ShowWorkflowProgressAsync(2, 2, $"创建Images目录: {imageName}");

            // Custom → Images：复制配置到Images目录
            var imageDir = await _directoryService.CreateImageFromCustomAsync(imageName, customDir);
            
            // 桩实现：模拟构建成功
            result.Success = true;
            result.ContainerName = $"deck_{imageName}";
            result.Messages.Add($"✅ 已从配置 {configName} 创建镜像目录: {imageName}");
            result.Messages.Add($"💡 镜像目录已准备完成，待容器服务实现后将自动构建");

            // 更新镜像元数据
            var metadata = new ImageMetadata
            {
                ImageName = imageName,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Environment.UserName,
                SourceConfig = customDir,
                BuildStatus = BuildStatus.Prepared // 使用准备完成状态
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
    public Task<ImagesWorkflowResult> ExecuteImagesWorkflowAsync(string imageName)
    {
        _logger.LogInformation("开始执行Images工作流程: {ImageName}", imageName);

        var result = new ImagesWorkflowResult
        {
            ImageName = imageName,
            Success = true, // 桩实现总是成功
            Action = ContainerAction.BuildAndStart,
            ContainerName = $"deck_{imageName}",
        };

        result.Messages.Add($"💡 Images工作流程桩实现: {imageName}");
        result.Messages.Add($"🔧 待容器服务实现后将提供完整的容器状态管理");

        return Task.FromResult(result);
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

    /// <summary>
    /// 执行直接构建工作流程
    /// Templates → Custom → Images → 构建
    /// </summary>
    private async Task ExecuteDirectBuildWorkflowAsync(string templateName, string envType, WorkflowExecutionResult result)
    {
        await ShowWorkflowProgressAsync(1, 3, $"步骤 1/3: 从模板 {templateName} 创建临时配置");
        
        // Step 1: Templates → Custom
        var customName = _directoryService.GenerateTimestampedName($"{templateName}-temp");
        if (!await _directoryService.CreateCustomFromTemplateAsync(templateName, customName, envType))
        {
            result.Errors.Add("创建临时配置失败");
            return;
        }

        await ShowWorkflowProgressAsync(2, 3, $"步骤 2/3: 创建镜像目录");
        
        // Step 2: Custom → Images  
        var imageName = _directoryService.GenerateImageName(customName);
        var customDir = _directoryService.GetCustomConfigPath(customName);
        var imageDir = await _directoryService.CreateImageFromCustomAsync(imageName, customDir);
        
        await ShowWorkflowProgressAsync(3, 3, $"步骤 3/3: 镜像目录准备完成");
        
        // Step 3: 桩实现 - 模拟构建成功
        result.Success = true;
        result.IsComplete = true;
        result.ImageName = imageName;
        result.CustomConfigName = customName;
        result.Messages.Add($"✅ 完整工作流程执行成功 (桩实现)");
        result.Messages.Add($"📦 镜像目录已创建: {imageName}");
        result.Messages.Add($"💡 待容器服务实现后将自动构建并启动容器");
        
        // 更新元数据
        var metadata = new ImageMetadata
        {
            ImageName = imageName,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Environment.UserName,
            SourceConfig = customDir,
            BuildStatus = BuildStatus.Prepared
        };
        await UpdateImageMetadataAsync(imageDir, metadata);
    }
}