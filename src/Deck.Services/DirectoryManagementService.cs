using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Deck.Core.Interfaces;
using Deck.Core.Models;
using Microsoft.Extensions.Logging;

namespace Deck.Services;

/// <summary>
/// 目录管理服务 - 对应 deck-shell 的 directory-mgmt.sh
/// 实现 .deck 目录的完整三层结构管理：templates、custom、images
/// </summary>
public class DirectoryManagementService : IDirectoryManagementService
{
    private readonly ILogger<DirectoryManagementService> _logger;
    private readonly IFileSystemService _fileSystemService;
    private readonly IImagePermissionService _imagePermissionService;

    // 目录常量 - 对应 deck-shell 的目录定义
    private const string DeckDirName = ".deck";
    private const string TemplatesDirName = "templates";
    private const string CustomDirName = "custom";
    private const string ImagesDirName = "images";
    private const string MetadataFileName = ".deck-metadata";
    private const string GitIgnoreFileName = ".gitignore";

    public DirectoryManagementService(
        ILogger<DirectoryManagementService> logger,
        IFileSystemService fileSystemService,
        IImagePermissionService imagePermissionService)
    {
        _logger = logger;
        _fileSystemService = fileSystemService;
        _imagePermissionService = imagePermissionService;
    }

    /// <summary>
    /// 初始化 .deck 目录结构 - 对应 deck-shell 的 create_dev_env_structure()
    /// </summary>
    public async Task InitializeDeckDirectoryAsync(string projectPath)
    {
        try
        {
            _logger.LogInformation("初始化 .deck 目录结构: {ProjectPath}", projectPath);

            var deckDir = Path.Combine(projectPath, DeckDirName);
            var templatesDir = Path.Combine(deckDir, TemplatesDirName);
            var customDir = Path.Combine(deckDir, CustomDirName);
            var imagesDir = Path.Combine(deckDir, ImagesDirName);

            // 创建三层目录结构
            await _fileSystemService.EnsureDirectoryExistsAsync(deckDir);
            await _fileSystemService.EnsureDirectoryExistsAsync(templatesDir);
            await _fileSystemService.EnsureDirectoryExistsAsync(customDir);
            await _fileSystemService.EnsureDirectoryExistsAsync(imagesDir);

            // 创建或更新 .gitignore 文件 - 对应 deck-shell 的 create_gitignore()
            await CreateOrUpdateGitIgnoreAsync(projectPath);

            _logger.LogInformation("成功初始化 .deck 目录结构");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化 .deck 目录结构失败");
            throw;
        }
    }

    /// <summary>
    /// 获取三层配置选项 - 对应 deck-shell 的 get_three_layer_options
    /// </summary>
    public async Task<ThreeLayerOptions> GetThreeLayerOptionsAsync()
    {
        try
        {
            var currentPath = Directory.GetCurrentDirectory();
            var deckDir = Path.Combine(currentPath, DeckDirName);

            var options = new ThreeLayerOptions();

            // 如果 .deck 目录不存在，先初始化
            if (!Directory.Exists(deckDir))
            {
                await InitializeDeckDirectoryAsync(currentPath);
            }

            // 获取 Images 层配置（已构建的镜像）
            options.Images = await GetImagesAsync();

            // 获取 Custom 层配置（用户自定义配置）
            options.Custom = await GetCustomConfigurationsAsync();

            // 获取 Templates 层配置（官方模板）
            options.Templates = await GetTemplatesAsync();

            return options;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取三层配置选项失败");
            throw;
        }
    }

    /// <summary>
    /// 验证目录结构完整性 - 对应 deck-shell 的 validate_directory_structure()
    /// </summary>
    public async Task<DirectoryStructureResult> ValidateDirectoryStructureAsync()
    {
        var result = new DirectoryStructureResult { IsValid = true };

        try
        {
            var currentPath = Directory.GetCurrentDirectory();
            var deckDir = Path.Combine(currentPath, DeckDirName);

            // 检查 .deck 根目录
            if (!Directory.Exists(deckDir))
            {
                result.IsValid = false;
                result.Errors.Add("缺少 .deck 目录");
                result.RepairSuggestions.Add("运行 'deck doctor' 或 'deck start' 自动创建目录结构");
                return result;
            }

            // 检查三层子目录
            var requiredDirs = new[]
            {
                (Path.Combine(deckDir, TemplatesDirName), "templates 目录"),
                (Path.Combine(deckDir, CustomDirName), "custom 目录"),
                (Path.Combine(deckDir, ImagesDirName), "images 目录")
            };

            foreach (var (dirPath, dirName) in requiredDirs)
            {
                if (!Directory.Exists(dirPath))
                {
                    result.IsValid = false;
                    result.Errors.Add($"缺少 {dirName}");
                    result.RepairSuggestions.Add($"创建目录: {dirPath}");
                }
            }

            // 检查 images 目录权限 - 集成权限管理服务
            var imagesDir = Path.Combine(deckDir, ImagesDirName);
            if (Directory.Exists(imagesDir))
            {
                var directories = Directory.GetDirectories(imagesDir);
                foreach (var imageDir in directories)
                {
                    var permissionSummary = await _imagePermissionService.GetImagePermissionSummaryAsync(imageDir);
                    if (!permissionSummary.IsValidImageDirectory)
                    {
                        result.Warnings.Add($"镜像目录 {Path.GetFileName(imageDir)} 不是有效的镜像目录");
                    }

                    if (permissionSummary.ProtectedFiles.Count > 0)
                    {
                        result.Warnings.Add($"镜像目录 {Path.GetFileName(imageDir)} 包含受保护的配置文件");
                        result.RepairSuggestions.Add($"检查镜像目录权限配置: {imageDir}");
                    }
                }
            }

            // 检查模板同步状态
            var templatesDir = Path.Combine(deckDir, TemplatesDirName);
            if (Directory.Exists(templatesDir) && !Directory.GetDirectories(templatesDir).Any())
            {
                result.Warnings.Add("templates 目录为空，可能需要同步远程模板");
                result.RepairSuggestions.Add("运行模板同步命令获取官方模板");
            }

            _logger.LogInformation("目录结构验证完成: {IsValid}", result.IsValid);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "目录结构验证失败");
            result.IsValid = false;
            result.Errors.Add($"验证过程出错: {ex.Message}");
            return result;
        }
    }

    /// <summary>
    /// 复制模板到自定义配置目录 - 对应 deck-shell 的 create_user_custom()
    /// </summary>
    public async Task<string> CopyTemplateToCustomAsync(string templateName, string? customName = null)
    {
        try
        {
            var currentPath = Directory.GetCurrentDirectory();
            var deckDir = Path.Combine(currentPath, DeckDirName);
            var customDir = Path.Combine(deckDir, CustomDirName);
            var templatePath = Path.Combine(deckDir, TemplatesDirName, templateName);

            // 检查模板是否存在
            if (!Directory.Exists(templatePath))
            {
                throw new DirectoryNotFoundException($"模板 '{templateName}' 不存在");
            }

            // 生成自定义配置名称 - 对应 deck-shell 的 generate_custom_name()
            var configName = customName ?? await GenerateCustomNameAsync(templateName);
            var targetPath = Path.Combine(customDir, configName);

            // 确保目标目录不存在
            if (Directory.Exists(targetPath))
            {
                throw new InvalidOperationException($"自定义配置 '{configName}' 已存在");
            }

            // 复制模板文件到 custom 目录（包括隐藏文件）
            await _fileSystemService.CopyDirectoryAsync(templatePath, targetPath, includeHiddenFiles: true);

            // 更新 PROJECT_NAME 环境变量
            var envFile = Path.Combine(targetPath, ".env");
            if (File.Exists(envFile))
            {
                await UpdateProjectNameInEnvAsync(envFile, configName);
            }

            _logger.LogInformation("成功复制模板 '{TemplateName}' 到自定义配置 '{ConfigName}'", templateName, configName);
            return configName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "复制模板到自定义配置失败: {TemplateName}", templateName);
            throw;
        }
    }

    /// <summary>
    /// 获取镜像元数据 - 对应 deck-shell 的镜像元数据读取
    /// </summary>
    [RequiresUnreferencedCode("JSON serialization may require types that cannot be statically analyzed")]
    public async Task<ImageMetadata?> GetImageMetadataAsync(string imageName)
    {
        try
        {
            var currentPath = Directory.GetCurrentDirectory();
            var imagesDir = Path.Combine(currentPath, DeckDirName, ImagesDirName);
            var imagePath = Path.Combine(imagesDir, imageName);
            var metadataPath = Path.Combine(imagePath, MetadataFileName);

            if (!File.Exists(metadataPath))
            {
                return null;
            }

            var jsonContent = await File.ReadAllTextAsync(metadataPath);
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var metadata = JsonSerializer.Deserialize<ImageMetadata>(jsonContent, options);

            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读取镜像元数据失败: {ImageName}", imageName);
            return null;
        }
    }

    /// <summary>
    /// 保存镜像元数据 - 对应 deck-shell 的元数据写入
    /// </summary>
    [RequiresUnreferencedCode("JSON serialization may require types that cannot be statically analyzed")]
    public async Task SaveImageMetadataAsync(ImageMetadata metadata)
    {
        try
        {
            var currentPath = Directory.GetCurrentDirectory();
            var imagesDir = Path.Combine(currentPath, DeckDirName, ImagesDirName);
            var imagePath = Path.Combine(imagesDir, metadata.ImageName);
            var metadataPath = Path.Combine(imagePath, MetadataFileName);

            // 确保镜像目录存在
            await _fileSystemService.EnsureDirectoryExistsAsync(imagePath);

            // 序列化并保存元数据
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var jsonContent = JsonSerializer.Serialize(metadata, options);
            await File.WriteAllTextAsync(metadataPath, jsonContent);

            _logger.LogDebug("保存镜像元数据: {ImageName}", metadata.ImageName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存镜像元数据失败: {ImageName}", metadata.ImageName);
            throw;
        }
    }

    /// <summary>
    /// 获取所有已构建的镜像列表 - Images 层
    /// </summary>
    public async Task<List<ConfigurationOption>> GetImagesAsync()
    {
        var images = new List<ConfigurationOption>();

        try
        {
            var currentPath = Directory.GetCurrentDirectory();
            var imagesDir = Path.Combine(currentPath, DeckDirName, ImagesDirName);

            if (!Directory.Exists(imagesDir))
            {
                return images;
            }

            var directories = Directory.GetDirectories(imagesDir);

            foreach (var dir in directories)
            {
                var imageName = Path.GetFileName(dir);
                
                // 验证镜像目录名格式 - 对应 deck-shell 的 validate_image_directory_name()
                if (!IsValidImageDirectoryName(imageName))
                {
                    _logger.LogWarning("忽略无效的镜像目录名: {ImageName}", imageName);
                    continue;
                }

                #pragma warning disable IL2026
                var metadata = await GetImageMetadataAsync(imageName);
                #pragma warning restore IL2026
                var lastModified = metadata?.LastStarted ?? Directory.GetLastWriteTime(dir);

                images.Add(new ConfigurationOption
                {
                    Name = imageName,
                    Type = ConfigurationType.Images,
                    Path = dir,
                    ProjectType = DetectProjectType(dir),
                    IsAvailable = true,
                    Description = $"{metadata?.BuildStatus ?? BuildStatus.Built} | {metadata?.ContainerName ?? "No container"}",
                    LastModified = lastModified,
                    Metadata = metadata
                });
            }

            return images.OrderByDescending(i => i.LastModified).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取镜像列表失败");
            return images;
        }
    }

    /// <summary>
    /// 根据名称获取镜像配置
    /// </summary>
    public async Task<ConfigurationOption?> GetImageByNameAsync(string imageName)
    {
        var images = await GetImagesAsync();
        return images.FirstOrDefault(i => i.Name == imageName);
    }

    #region Private Helper Methods

    /// <summary>
    /// 获取 Custom 层配置（用户自定义配置）
    /// </summary>
    private Task<List<ConfigurationOption>> GetCustomConfigurationsAsync()
    {
        var customs = new List<ConfigurationOption>();

        try
        {
            var currentPath = Directory.GetCurrentDirectory();
            var customDir = Path.Combine(currentPath, DeckDirName, CustomDirName);

            if (!Directory.Exists(customDir))
            {
                return Task.FromResult(customs);
            }

            var directories = Directory.GetDirectories(customDir);

            foreach (var dir in directories)
            {
                var configName = Path.GetFileName(dir);
                var lastModified = Directory.GetLastWriteTime(dir);

                customs.Add(new ConfigurationOption
                {
                    Name = configName,
                    Type = ConfigurationType.Custom,
                    Path = dir,
                    ProjectType = DetectProjectType(dir),
                    IsAvailable = true,
                    Description = "用户自定义配置",
                    LastModified = lastModified
                });
            }

            return Task.FromResult(customs.OrderByDescending(c => c.LastModified).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取自定义配置列表失败");
            return Task.FromResult(customs);
        }
    }

    /// <summary>
    /// 获取 Templates 层配置（官方模板）
    /// </summary>
    private Task<List<ConfigurationOption>> GetTemplatesAsync()
    {
        var templates = new List<ConfigurationOption>();

        try
        {
            var currentPath = Directory.GetCurrentDirectory();
            var templatesDir = Path.Combine(currentPath, DeckDirName, TemplatesDirName);

            if (!Directory.Exists(templatesDir))
            {
                return Task.FromResult(templates);
            }

            var directories = Directory.GetDirectories(templatesDir);

            foreach (var dir in directories)
            {
                var templateName = Path.GetFileName(dir);
                var lastModified = Directory.GetLastWriteTime(dir);

                templates.Add(new ConfigurationOption
                {
                    Name = templateName,
                    Type = ConfigurationType.Templates,
                    Path = dir,
                    ProjectType = DetectProjectType(dir),
                    IsAvailable = true,
                    Description = "官方模板配置",
                    LastModified = lastModified
                });
            }

            return Task.FromResult(templates.OrderBy(t => t.Name).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取模板列表失败");
            return Task.FromResult(templates);
        }
    }

    /// <summary>
    /// 生成自定义配置名称 - 对应 deck-shell 的 generate_custom_name()
    /// </summary>
    private Task<string> GenerateCustomNameAsync(string templateName)
    {
        var currentPath = Directory.GetCurrentDirectory();
        var customDir = Path.Combine(currentPath, DeckDirName, CustomDirName);

        var baseName = $"{templateName}-custom";
        var counter = 1;
        var configName = baseName;

        while (Directory.Exists(Path.Combine(customDir, configName)))
        {
            configName = $"{baseName}-{counter:D2}";
            counter++;
        }

        return Task.FromResult(configName);
    }

    /// <summary>
    /// 创建或更新 .gitignore 文件 - 对应 deck-shell 的 create_gitignore()
    /// </summary>
    private async Task CreateOrUpdateGitIgnoreAsync(string projectPath)
    {
        try
        {
            var gitIgnorePath = Path.Combine(projectPath, GitIgnoreFileName);
            var deckIgnoreRules = new[]
            {
                "# Deck 开发环境管理工具",
                ".deck/templates/",
                ".deck/images/",
                "# 保留 .deck/custom/ 用于版本控制"
            };

            var existingContent = "";
            if (File.Exists(gitIgnorePath))
            {
                existingContent = await File.ReadAllTextAsync(gitIgnorePath);
            }

            // 检查是否已经包含 Deck 相关规则
            if (!existingContent.Contains(".deck/templates/"))
            {
                var updatedContent = existingContent;
                if (!string.IsNullOrEmpty(existingContent) && !existingContent.EndsWith('\n'))
                {
                    updatedContent += Environment.NewLine;
                }

                updatedContent += Environment.NewLine + string.Join(Environment.NewLine, deckIgnoreRules) + Environment.NewLine;
                await File.WriteAllTextAsync(gitIgnorePath, updatedContent);

                _logger.LogInformation("已更新 .gitignore 文件");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "更新 .gitignore 文件失败");
        }
    }

    /// <summary>
    /// 更新环境变量文件中的 PROJECT_NAME - 对应 deck-shell 的 PROJECT_NAME 更新逻辑
    /// </summary>
    private async Task UpdateProjectNameInEnvAsync(string envFilePath, string projectName)
    {
        try
        {
            var lines = await File.ReadAllLinesAsync(envFilePath);
            var updatedLines = new List<string>();
            var projectNameUpdated = false;

            foreach (var line in lines)
            {
                if (line.StartsWith("PROJECT_NAME="))
                {
                    updatedLines.Add($"PROJECT_NAME={projectName}");
                    projectNameUpdated = true;
                }
                else
                {
                    updatedLines.Add(line);
                }
            }

            // 如果没有 PROJECT_NAME，添加一个
            if (!projectNameUpdated)
            {
                updatedLines.Add($"PROJECT_NAME={projectName}");
            }

            await File.WriteAllLinesAsync(envFilePath, updatedLines);
            _logger.LogDebug("已更新环境变量文件中的 PROJECT_NAME: {ProjectName}", projectName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "更新 PROJECT_NAME 失败: {EnvFile}", envFilePath);
        }
    }

    /// <summary>
    /// 验证镜像目录名格式 - 对应 deck-shell 的 validate_image_directory_name()
    /// </summary>
    private static bool IsValidImageDirectoryName(string imageName)
    {
        // 镜像目录名应该包含时间戳格式: template-YYYYMMDD-HHMMSS
        if (string.IsNullOrEmpty(imageName))
            return false;

        // 基本格式检查：包含至少一个连字符
        if (!imageName.Contains('-'))
            return false;

        // 可以添加更严格的正则表达式验证
        return true;
    }

    /// <summary>
    /// 检测项目类型 - 基于目录内容推断
    /// </summary>
    private static ProjectType DetectProjectType(string directoryPath)
    {
        try
        {
            var files = Directory.GetFiles(directoryPath, "*", SearchOption.TopDirectoryOnly);

            if (files.Any(f => Path.GetFileName(f) == "tauri.conf.json"))
                return ProjectType.Tauri;

            if (files.Any(f => Path.GetFileName(f) == "pubspec.yaml"))
                return ProjectType.Flutter;

            if (files.Any(f => Path.GetFileName(f).EndsWith(".sln") || Path.GetFileName(f).EndsWith(".csproj")))
                return ProjectType.Avalonia;

            return ProjectType.Unknown;
        }
        catch
        {
            return ProjectType.Unknown;
        }
    }

    #endregion

    #region Three Layer Workflow Methods

    /// <summary>
    /// 生成唯一的Custom配置名称
    /// </summary>
    public string GenerateUniqueCustomName(string baseName)
    {
        var currentPath = Directory.GetCurrentDirectory();
        var customDir = Path.Combine(currentPath, DeckDirName, CustomDirName);

        var configName = $"{baseName}-001";
        var counter = 1;

        while (Directory.Exists(Path.Combine(customDir, configName)))
        {
            counter++;
            configName = $"{baseName}-{counter:D3}";
        }

        return configName;
    }

    /// <summary>
    /// 生成带时间戳的名称
    /// </summary>
    public string GenerateTimestampedName(string baseName)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmm");
        return $"{baseName}-{timestamp}";
    }

    /// <summary>
    /// 生成带时间戳的镜像名称
    /// </summary>
    public string GenerateTimestampedImageName(string baseName)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmm");
        return $"{baseName}-{timestamp}";
    }

    /// <summary>
    /// 生成镜像名称
    /// </summary>
    public string GenerateImageName(string baseName)
    {
        return $"{baseName}-build";
    }

    /// <summary>
    /// 获取模板目录路径
    /// </summary>
    public string GetTemplateDirectory(string templateName)
    {
        var currentPath = Directory.GetCurrentDirectory();
        return Path.Combine(currentPath, DeckDirName, TemplatesDirName, templateName);
    }

    /// <summary>
    /// 获取Custom配置路径
    /// </summary>
    public string GetCustomConfigPath(string configName)
    {
        var currentPath = Directory.GetCurrentDirectory();
        return Path.Combine(currentPath, DeckDirName, CustomDirName, configName);
    }

    /// <summary>
    /// 获取镜像目录路径
    /// </summary>
    public string GetImageDirectory(string imageName)
    {
        var currentPath = Directory.GetCurrentDirectory();
        return Path.Combine(currentPath, DeckDirName, ImagesDirName, imageName);
    }

    /// <summary>
    /// 从模板创建Custom配置
    /// </summary>
    public async Task<bool> CreateCustomFromTemplateAsync(string templateName, string customName, string envType)
    {
        try
        {
            var templateDir = GetTemplateDirectory(templateName);
            var customDir = GetCustomConfigPath(customName);

            if (!Directory.Exists(templateDir))
            {
                _logger.LogError("模板目录不存在: {TemplateDir}", templateDir);
                return false;
            }

            // 创建目标目录
            Directory.CreateDirectory(customDir);

            // 复制所有文件，包括隐藏文件
            await _fileSystemService.CopyDirectoryAsync(templateDir, customDir, true);

            // 更新PROJECT_NAME避免冲突
            var envFile = Path.Combine(customDir, ".env");
            if (File.Exists(envFile))
            {
                await UpdateProjectNameInEnvAsync(envFile, customName);
            }

            _logger.LogInformation("已从模板创建Custom配置: {TemplateName} -> {CustomName}", templateName, customName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从模板创建Custom配置失败: {TemplateName}", templateName);
            return false;
        }
    }

    /// <summary>
    /// 从Custom配置创建镜像目录
    /// </summary>
    public async Task<string> CreateImageFromCustomAsync(string imageName, string customDir)
    {
        try
        {
            var imageDir = GetImageDirectory(imageName);

            if (!Directory.Exists(customDir))
            {
                throw new DirectoryNotFoundException($"Custom配置目录不存在: {customDir}");
            }

            // 创建镜像目录
            Directory.CreateDirectory(imageDir);

            // 复制配置文件到镜像目录
            await _fileSystemService.CopyDirectoryAsync(customDir, imageDir, true);

            // 更新PROJECT_NAME为唯一值
            var envFile = Path.Combine(imageDir, ".env");
            if (File.Exists(envFile))
            {
                await UpdateProjectNameInEnvAsync(envFile, imageName);
            }

            _logger.LogInformation("已从Custom配置创建镜像目录: {CustomDir} -> {ImageName}", customDir, imageName);
            return imageDir;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从Custom配置创建镜像目录失败: {ImageName}", imageName);
            throw;
        }
    }

    #endregion
}