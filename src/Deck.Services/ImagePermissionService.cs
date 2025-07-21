using Deck.Core.Interfaces;
using Deck.Core.Models;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.RegularExpressions;

namespace Deck.Services;

/// <summary>
/// 镜像权限管理服务 - 对应 deck-shell 的 directory-mgmt.sh 权限管理功能
/// </summary>
public class ImagePermissionService : IImagePermissionService
{
    private readonly ILogger<ImagePermissionService> _logger;

    // 运行时可修改的环境变量（对应 deck-shell 的 RUNTIME_VARS）
    private static readonly HashSet<string> RuntimeVariables = new(StringComparer.OrdinalIgnoreCase)
    {
        "DEV_PORT", "DEBUG_PORT", "PROJECT_NAME", 
        "WORKSPACE_PATH", "CONTAINER_NAME", "NETWORK_NAME", "VOLUME_PREFIX"
    };

    // 受保护的配置文件
    private static readonly HashSet<string> ProtectedConfigFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "compose.yaml", "compose.yml", "docker-compose.yaml", "docker-compose.yml",
        "Dockerfile", "Dockerfile.dev", "Dockerfile.prod", 
        ".dockerignore", "metadata.json"
    };

    // 镜像目录名称格式：prefix-YYYYMMDD-HHMM
    private static readonly Regex ImageDirectoryNamePattern = new(
        @"^(?<prefix>[a-zA-Z0-9_-]+)-(?<date>\d{8})-(?<time>\d{4})$", 
        RegexOptions.Compiled);

    public ImagePermissionService(ILogger<ImagePermissionService> logger)
    {
        _logger = logger;
    }

    public Task<FilePermissionResult> ValidateFilePermissionAsync(string imagePath, string filePath, FileOperation operation)
    {
        _logger.LogDebug("验证文件权限: {ImagePath}/{FilePath}, 操作: {Operation}", imagePath, filePath, operation);

        var result = new FilePermissionResult
        {
            FilePath = filePath,
            Operation = operation
        };

        try
        {
            // 标准化文件路径
            var normalizedPath = Path.GetFileName(filePath);
            
            // 检查是否为受保护的配置文件
            if (IsProtectedConfigFile(normalizedPath))
            {
                HandleProtectedFileOperation(result, normalizedPath, operation);
                return Task.FromResult(result);
            }

            // 检查 .env 文件的特殊权限
            if (string.Equals(normalizedPath, ".env", StringComparison.OrdinalIgnoreCase))
            {
                HandleEnvFileOperation(result, operation);
                return Task.FromResult(result);
            }

            // 检查其他文件类型
            HandleRegularFileOperation(result, normalizedPath, operation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件权限验证失败: {FilePath}", filePath);
            result.Permission = PermissionLevel.Denied;
            result.Reason = $"权限验证出错: {ex.Message}";
        }

        return Task.FromResult(result);
    }

    public Task<DirectoryPermissionResult> ValidateDirectoryOperationAsync(string imagePath, DirectoryOperation operation)
    {
        _logger.LogDebug("验证目录权限: {ImagePath}, 操作: {Operation}", imagePath, operation);

        var result = new DirectoryPermissionResult
        {
            DirectoryPath = imagePath,
            Operation = operation
        };

        try
        {
            switch (operation)
            {
                case DirectoryOperation.Read:
                    // 读取操作始终允许
                    result.Permission = PermissionLevel.Allowed;
                    result.Reason = "读取操作不受限制";
                    break;

                case DirectoryOperation.Create:
                    // 在镜像目录中创建文件需要特殊检查
                    result.Permission = PermissionLevel.Warning;
                    result.Reason = "可以创建文件，但建议遵循目录结构规范";
                    result.Suggestions.Add("避免创建临时文件，使用 .gitignore 排除非必需文件");
                    break;

                case DirectoryOperation.Delete:
                    // 删除镜像目录是高风险操作
                    result.Permission = PermissionLevel.Denied;
                    result.Reason = "删除镜像目录会破坏镜像-目录映射关系";
                    result.Impact = new DirectoryOperationImpact
                    {
                        Level = ImpactLevel.Critical,
                        Description = "删除镜像目录将导致无法找到对应的容器镜像",
                        AffectedComponents = new List<string> { "镜像管理", "容器启动", "配置加载" },
                        Risks = new List<string> { "数据丢失", "环境配置丢失", "构建历史丢失" }
                    };
                    result.Suggestions.Add("如需重新开始，使用 deck clean 命令");
                    break;

                case DirectoryOperation.Rename:
                    // 重命名镜像目录是严格禁止的
                    result.Permission = PermissionLevel.Denied;
                    result.Reason = "重命名镜像目录会破坏镜像名称与目录的对应关系";
                    result.Impact = new DirectoryOperationImpact
                    {
                        Level = ImpactLevel.Critical,
                        Description = "重命名会导致系统无法找到镜像配置",
                        AffectedComponents = new List<string> { "镜像识别", "自动启动", "配置管理" },
                        Risks = new List<string> { "镜像无法启动", "配置无法加载", "系统状态不一致" }
                    };
                    result.Alternatives.Add("使用 Custom/ 目录创建新的配置变体");
                    result.Alternatives.Add("通过 Templates/ 创建新的项目模板");
                    break;

                case DirectoryOperation.ModifyPermissions:
                    // 修改权限操作需要谨慎
                    result.Permission = PermissionLevel.Warning;
                    result.Reason = "修改目录权限可能影响容器访问";
                    result.Suggestions.Add("确保容器用户有足够权限访问文件");
                    break;

                default:
                    result.Permission = PermissionLevel.Denied;
                    result.Reason = "未知操作类型";
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "目录权限验证失败: {ImagePath}", imagePath);
            result.Permission = PermissionLevel.Denied;
            result.Reason = $"权限验证出错: {ex.Message}";
        }

        return Task.FromResult(result);
    }

    public Task<EnvPermissionResult> ValidateEnvFileChangesAsync(string imagePath, Dictionary<string, string?> envChanges)
    {
        _logger.LogDebug("验证环境变量权限: {ImagePath}, 变量数量: {Count}", imagePath, envChanges.Count);

        var result = new EnvPermissionResult();

        try
        {
            foreach (var kvp in envChanges)
            {
                var validation = ValidateEnvironmentVariable(kvp.Key, kvp.Value);
                result.ValidationDetails.Add(validation);

                switch (validation.Permission)
                {
                    case PermissionLevel.Allowed:
                        result.AllowedChanges[kvp.Key] = kvp.Value ?? string.Empty;
                        break;
                    case PermissionLevel.Warning:
                        result.WarningChanges[kvp.Key] = kvp.Value ?? string.Empty;
                        break;
                    case PermissionLevel.Denied:
                        result.DeniedChanges[kvp.Key] = kvp.Value ?? string.Empty;
                        break;
                }
            }

            // IsValid is now a computed property based on DeniedChanges.Count
            result.Summary = GenerateEnvValidationSummary(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "环境变量权限验证失败: {ImagePath}", imagePath);
            result.Summary = $"验证失败: {ex.Message}";
        }

        return Task.FromResult(result);
    }

    public Task<ImagePermissionSummary> GetImagePermissionSummaryAsync(string imagePath)
    {
        _logger.LogDebug("获取镜像权限概况: {ImagePath}", imagePath);

        var summary = new ImagePermissionSummary
        {
            ImagePath = imagePath,
            ImageName = Path.GetFileName(imagePath)
        };

        try
        {
            // 验证是否为有效的镜像目录
            summary.IsValidImageDirectory = ValidateImageDirectoryStructure(imagePath);

            // 扫描受保护的文件
            ScanProtectedFiles(imagePath, summary);

            // 扫描可修改的文件
            ScanModifiableFiles(imagePath, summary);

            // 设置环境变量列表
            summary.RuntimeVariables.AddRange(RuntimeVariables);
            
            // 生成策略说明和最佳实践
            GeneratePolicyGuidance(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取镜像权限概况失败: {ImagePath}", imagePath);
            summary.IsValidImageDirectory = false;
        }

        return Task.FromResult(summary);
    }

    public Task<DirectoryNameValidationResult> ValidateImageDirectoryNameAsync(string directoryName)
    {
        _logger.LogDebug("验证目录名称: {DirectoryName}", directoryName);

        var result = new DirectoryNameValidationResult
        {
            DirectoryName = directoryName,
            FormatDescription = "镜像目录名称格式: prefix-YYYYMMDD-HHMM (例如: myapp-20240315-1430)"
        };

        try
        {
            var match = ImageDirectoryNamePattern.Match(directoryName);
            if (match.Success)
            {
                result.IsValid = true;
                result.ParsedInfo = new DirectoryNameInfo
                {
                    Prefix = match.Groups["prefix"].Value,
                    TimeStamp = $"{match.Groups["date"].Value}-{match.Groups["time"].Value}",
                    IsStandardFormat = true
                };

                // 尝试解析时间
                if (DateTime.TryParseExact(
                    $"{match.Groups["date"].Value} {match.Groups["time"].Value}",
                    "yyyyMMdd HHmm",
                    null,
                    System.Globalization.DateTimeStyles.None,
                    out var parsedTime))
                {
                    result.ParsedInfo.CreationTime = parsedTime;
                }
            }
            else
            {
                result.IsValid = false;
                AnalyzeDirectoryNameIssues(directoryName, result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "目录名称验证失败: {DirectoryName}", directoryName);
            result.IsValid = false;
            result.ValidationErrors.Add($"验证过程出错: {ex.Message}");
        }

        return Task.FromResult(result);
    }

    public Task<List<string>> GetRuntimeModifiableVariablesAsync()
    {
        return Task.FromResult(RuntimeVariables.ToList());
    }

    public Task<bool> IsProtectedConfigFileAsync(string fileName)
    {
        return Task.FromResult(IsProtectedConfigFile(fileName));
    }

    public Task<PermissionGuidance> GetPermissionGuidanceAsync(PermissionViolation violation)
    {
        _logger.LogDebug("获取权限指导: {Type} - {Path}", violation.Type, violation.Path);

        var guidance = new PermissionGuidance
        {
            Violation = violation
        };

        try
        {
            switch (violation.Type)
            {
                case ViolationType.ProtectedFileModification:
                    GenerateProtectedFileGuidance(guidance);
                    break;
                case ViolationType.DirectoryRename:
                    GenerateDirectoryRenameGuidance(guidance);
                    break;
                case ViolationType.BuildTimeVariableModification:
                    GenerateBuildTimeVariableGuidance(guidance);
                    break;
                case ViolationType.CoreFileDeletion:
                    GenerateCoreFileDeletionGuidance(guidance);
                    break;
                case ViolationType.InvalidDirectoryName:
                    GenerateInvalidDirectoryNameGuidance(guidance);
                    break;
                default:
                    guidance.DetailedExplanation = "未知的权限违规类型";
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成权限指导失败: {Type}", violation.Type);
            guidance.DetailedExplanation = $"生成指导信息时出错: {ex.Message}";
        }

        return Task.FromResult(guidance);
    }

    #region Private Methods

    private bool IsProtectedConfigFile(string fileName)
    {
        return ProtectedConfigFiles.Contains(fileName);
    }

    private void HandleProtectedFileOperation(FilePermissionResult result, string fileName, FileOperation operation)
    {
        switch (operation)
        {
            case FileOperation.Read:
                result.Permission = PermissionLevel.Allowed;
                result.Reason = "读取受保护文件是允许的";
                break;
                
            case FileOperation.Write:
            case FileOperation.Delete:
            case FileOperation.Move:
                result.Permission = PermissionLevel.Denied;
                result.Reason = $"{fileName} 是受保护的配置快照，不允许修改";
                result.Suggestions.Add("此文件是构建时配置的快照，确保环境一致性");
                result.Alternatives.Add("如需修改构建配置，请在 Custom/ 目录中创建新版本");
                result.Alternatives.Add("如需修改运行时配置，请编辑 .env 文件中的运行时变量");
                break;
                
            case FileOperation.Create:
                result.Permission = PermissionLevel.Denied;
                result.Reason = $"不能创建与受保护文件同名的文件: {fileName}";
                break;
                
            default:
                result.Permission = PermissionLevel.Denied;
                result.Reason = "未知操作";
                break;
        }
    }

    private void HandleEnvFileOperation(FilePermissionResult result, FileOperation operation)
    {
        switch (operation)
        {
            case FileOperation.Read:
                result.Permission = PermissionLevel.Allowed;
                result.Reason = "读取 .env 文件是允许的";
                break;
                
            case FileOperation.Write:
                result.Permission = PermissionLevel.Warning;
                result.Reason = ".env 文件可以修改，但仅限运行时变量";
                result.Suggestions.Add("只修改运行时变量: " + string.Join(", ", RuntimeVariables));
                result.Suggestions.Add("构建时变量的修改不会影响已构建的镜像");
                result.Alternatives.Add("如需修改构建时变量，请在 Custom/ 目录中重新配置");
                break;
                
            case FileOperation.Delete:
                result.Permission = PermissionLevel.Denied;
                result.Reason = "删除 .env 文件会导致环境变量丢失";
                result.Suggestions.Add("如需重置环境变量，请编辑文件内容而不是删除");
                break;
                
            case FileOperation.Move:
                result.Permission = PermissionLevel.Denied;
                result.Reason = "移动 .env 文件会破坏配置结构";
                break;
                
            default:
                result.Permission = PermissionLevel.Warning;
                result.Reason = "请谨慎操作 .env 文件";
                break;
        }
    }

    private void HandleRegularFileOperation(FilePermissionResult result, string fileName, FileOperation operation)
    {
        // 对于普通文件，大多数操作是允许的
        switch (operation)
        {
            case FileOperation.Read:
                result.Permission = PermissionLevel.Allowed;
                result.Reason = "读取普通文件是允许的";
                break;
                
            case FileOperation.Write:
            case FileOperation.Create:
                result.Permission = PermissionLevel.Allowed;
                result.Reason = "可以修改或创建普通文件";
                result.Suggestions.Add("建议将自定义文件添加到版本控制");
                break;
                
            case FileOperation.Delete:
                result.Permission = PermissionLevel.Warning;
                result.Reason = "删除文件需要谨慎，确保不影响项目功能";
                result.Suggestions.Add("建议先备份重要文件");
                break;
                
            case FileOperation.Move:
                result.Permission = PermissionLevel.Warning;
                result.Reason = "移动文件可能影响引用路径";
                result.Suggestions.Add("确保更新所有引用此文件的配置");
                break;
                
            default:
                result.Permission = PermissionLevel.Allowed;
                result.Reason = "普通文件操作通常是允许的";
                break;
        }
    }

    private EnvVariableValidation ValidateEnvironmentVariable(string variableName, string? newValue)
    {
        var validation = new EnvVariableValidation
        {
            VariableName = variableName,
            NewValue = newValue
        };

        // 检查是否为运行时变量
        if (RuntimeVariables.Contains(variableName))
        {
            validation.Permission = PermissionLevel.Allowed;
            validation.VariableType = EnvVariableType.Runtime;
            validation.Reason = "运行时变量，允许在镜像目录中修改";
        }
        else if (IsSystemVariable(variableName))
        {
            validation.Permission = PermissionLevel.Denied;
            validation.VariableType = EnvVariableType.System;
            validation.Reason = "系统变量，受保护不允许修改";
            validation.Suggestion = "系统变量由 deck 工具自动管理";
        }
        else
        {
            validation.Permission = PermissionLevel.Denied;
            validation.VariableType = EnvVariableType.BuildTime;
            validation.Reason = "构建时变量，只能在 Custom/ 目录中修改";
            validation.Suggestion = "在 Custom/ 目录中创建新的配置版本来修改此变量";
        }

        return validation;
    }

    private bool IsSystemVariable(string variableName)
    {
        var systemVariables = new[] { "PATH", "HOME", "USER", "SHELL" };
        return systemVariables.Any(v => string.Equals(v, variableName, StringComparison.OrdinalIgnoreCase));
    }

    private string GenerateEnvValidationSummary(EnvPermissionResult result)
    {
        var parts = new List<string>();

        if (result.AllowedChanges.Count > 0)
            parts.Add($"{result.AllowedChanges.Count} 个变量允许修改");

        if (result.WarningChanges.Count > 0)
            parts.Add($"{result.WarningChanges.Count} 个变量需要注意");

        if (result.DeniedChanges.Count > 0)
            parts.Add($"{result.DeniedChanges.Count} 个变量被拒绝");

        return string.Join(", ", parts);
    }

    private bool ValidateImageDirectoryStructure(string imagePath)
    {
        try
        {
            if (!Directory.Exists(imagePath))
                return false;

            // 检查是否有基本的镜像文件
            var files = Directory.GetFiles(imagePath);
            return files.Any(f => ProtectedConfigFiles.Contains(Path.GetFileName(f), StringComparer.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    private void ScanProtectedFiles(string imagePath, ImagePermissionSummary summary)
    {
        try
        {
            if (!Directory.Exists(imagePath))
                return;

            var files = Directory.GetFiles(imagePath);
            foreach (var filePath in files)
            {
                var fileName = Path.GetFileName(filePath);
                if (IsProtectedConfigFile(fileName))
                {
                    var protectedFile = new ProtectedFile
                    {
                        FilePath = fileName,
                        FileType = GetProtectedFileType(fileName),
                        ProtectionReason = "此文件是构建配置的快照，确保环境一致性"
                    };

                    protectedFile.Alternatives.Add("在 Custom/ 目录中创建新的配置版本");
                    protectedFile.Alternatives.Add("通过 Templates/ 系统管理项目模板");
                    
                    summary.ProtectedFiles.Add(protectedFile);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "扫描受保护文件失败: {ImagePath}", imagePath);
        }
    }

    private void ScanModifiableFiles(string imagePath, ImagePermissionSummary summary)
    {
        try
        {
            if (!Directory.Exists(imagePath))
                return;

            var files = Directory.GetFiles(imagePath);
            foreach (var filePath in files)
            {
                var fileName = Path.GetFileName(filePath);
                if (!IsProtectedConfigFile(fileName))
                {
                    var modifiableFile = new ModifiableFile
                    {
                        FilePath = fileName
                    };

                    if (string.Equals(fileName, ".env", StringComparison.OrdinalIgnoreCase))
                    {
                        modifiableFile.AllowedOperations.AddRange(new[] { FileOperation.Read, FileOperation.Write });
                        modifiableFile.ModificationGuidelines.Add("只修改运行时变量");
                        modifiableFile.ModificationGuidelines.Add("构建时变量修改不会影响已构建镜像");
                    }
                    else
                    {
                        modifiableFile.AllowedOperations.AddRange(Enum.GetValues<FileOperation>());
                        modifiableFile.ModificationGuidelines.Add("请遵循项目结构约定");
                        modifiableFile.ModificationGuidelines.Add("建议将重要修改纳入版本控制");
                    }

                    summary.ModifiableFiles.Add(modifiableFile);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "扫描可修改文件失败: {ImagePath}", imagePath);
        }
    }

    private ProtectedFileType GetProtectedFileType(string fileName)
    {
        return fileName.ToLowerInvariant() switch
        {
            var name when name.Contains("compose") => ProtectedFileType.ComposeConfig,
            var name when name.StartsWith("dockerfile") => ProtectedFileType.Dockerfile,
            "metadata.json" => ProtectedFileType.Metadata,
            ".dockerignore" => ProtectedFileType.SystemConfig,
            _ => ProtectedFileType.SystemConfig
        };
    }

    private void GeneratePolicyGuidance(ImagePermissionSummary summary)
    {
        summary.PolicyDescription = "镜像目录采用三层架构权限模型：Templates（模板）→ Custom（定制）→ Images（镜像实例）";

        summary.BestPractices.AddRange(new[]
        {
            "镜像目录中的配置文件是构建时快照，不应直接修改",
            "运行时配置通过 .env 文件中的运行时变量调整",
            "构建时配置修改应在 Custom/ 目录中进行",
            "避免重命名镜像目录，这会破坏镜像-目录映射关系",
            "使用版本控制管理自定义文件和配置变更"
        });
    }

    private void AnalyzeDirectoryNameIssues(string directoryName, DirectoryNameValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(directoryName))
        {
            result.ValidationErrors.Add("目录名称不能为空");
            return;
        }

        if (!directoryName.Contains('-'))
        {
            result.ValidationErrors.Add("目录名称格式错误，应包含连字符分隔符");
            result.SuggestedName = $"{directoryName}-{DateTime.Now:yyyyMMdd-HHmm}";
        }
        else
        {
            var parts = directoryName.Split('-');
            if (parts.Length < 3)
            {
                result.ValidationErrors.Add("目录名称组成部分不足，应为: prefix-YYYYMMDD-HHMM");
            }
            else
            {
                if (parts[1].Length != 8 || !parts[1].All(char.IsDigit))
                {
                    result.ValidationErrors.Add("日期部分格式错误，应为 YYYYMMDD 格式");
                }
                
                if (parts[2].Length != 4 || !parts[2].All(char.IsDigit))
                {
                    result.ValidationErrors.Add("时间部分格式错误，应为 HHMM 格式");
                }
            }

            // 尝试生成建议名称
            if (parts.Length >= 1)
            {
                result.SuggestedName = $"{parts[0]}-{DateTime.Now:yyyyMMdd-HHmm}";
            }
        }
    }

    private void GenerateProtectedFileGuidance(PermissionGuidance guidance)
    {
        guidance.DetailedExplanation = "受保护的配置文件是构建时配置的快照，用于确保环境一致性。直接修改这些文件会破坏 deck 工具的版本管理和环境隔离机制。";
        
        guidance.DesignRationale = "三层架构设计确保了配置的可追溯性和环境的可重现性。Templates 提供基础模板，Custom 允许定制化，Images 存储特定实例的快照。";

        guidance.FixSteps.AddRange(new[]
        {
            "确定要修改的配置类型（构建时 vs 运行时）",
            "如果是运行时配置，编辑 .env 文件中的相应变量",
            "如果是构建时配置，在 Custom/ 目录中创建新的配置版本",
            "使用 deck build 重新构建镜像以应用构建时配置更改"
        });

        guidance.Alternatives.AddRange(new[]
        {
            "编辑 .env 文件修改运行时变量",
            "在 Custom/ 目录创建配置变体",
            "通过 Templates/ 创建新的项目模板",
            "使用 deck config 命令进行配置管理"
        });

        guidance.Examples.AddRange(new[]
        {
            "# 修改运行时端口\necho 'DEV_PORT=3001' >> .env",
            "# 创建自定义配置\ncp -r Templates/myapp Custom/myapp-custom",
            "# 重新构建镜像\ndeck build myapp"
        });
    }

    private void GenerateDirectoryRenameGuidance(PermissionGuidance guidance)
    {
        guidance.DetailedExplanation = "镜像目录名称与 Docker 镜像名称有直接映射关系。重命名目录会导致系统无法找到对应的镜像，破坏自动化工作流程。";
        
        guidance.DesignRationale = "固定的命名约定确保了镜像管理的自动化和一致性，避免了手动维护映射关系的复杂性。";

        guidance.FixSteps.AddRange(new[]
        {
            "不要直接重命名镜像目录",
            "如需不同配置，在 Custom/ 目录中创建新版本",
            "使用 deck clean 清理不需要的镜像和目录",
            "通过 deck build 创建具有新名称的镜像"
        });

        guidance.Alternatives.AddRange(new[]
        {
            "在 Custom/ 目录创建新的配置版本",
            "使用 deck build --name 指定新的镜像名称",
            "通过环境变量 PROJECT_NAME 在运行时更改显示名称"
        });
    }

    private void GenerateBuildTimeVariableGuidance(PermissionGuidance guidance)
    {
        guidance.DetailedExplanation = "构建时变量影响 Docker 镜像的构建过程，在镜像目录中修改这些变量不会影响已构建的镜像。";
        
        guidance.DesignRationale = "区分构建时和运行时变量确保了环境配置的清晰性和可预测性。";

        guidance.FixSteps.AddRange(new[]
        {
            "确认变量是否真的需要在构建时修改",
            "在 Custom/ 目录中创建新的配置版本",
            "修改 Custom/ 目录中的 .env 文件",
            "重新构建镜像以应用更改"
        });

        guidance.Examples.AddRange(new[]
        {
            "# 运行时变量（可在镜像目录修改）",
            string.Join(", ", RuntimeVariables),
            "",
            "# 构建时变量（需在 Custom/ 修改）",
            "NODE_VERSION, PYTHON_VERSION, BASE_IMAGE 等"
        });
    }

    private void GenerateCoreFileDeletionGuidance(PermissionGuidance guidance)
    {
        guidance.DetailedExplanation = "核心配置文件是镜像正常运行的基础，删除这些文件会导致容器无法启动或功能异常。";
        
        guidance.FixSteps.AddRange(new[]
        {
            "不要删除 compose.yaml、Dockerfile 等核心文件",
            "如需重新配置，使用 deck clean 清理整个环境",
            "从 Custom/ 或 Templates/ 重新创建配置"
        });
    }

    private void GenerateInvalidDirectoryNameGuidance(PermissionGuidance guidance)
    {
        guidance.DetailedExplanation = "镜像目录名称必须遵循 prefix-YYYYMMDD-HHMM 格式，这确保了创建时间的可追溯性和目录的唯一性。";
        
        guidance.FixSteps.AddRange(new[]
        {
            "使用正确的命名格式：prefix-YYYYMMDD-HHMM",
            "确保日期和时间部分为数字",
            "避免使用特殊字符（除了连字符）"
        });

        guidance.Examples.AddRange(new[]
        {
            "myapp-20240315-1430  # 正确格式",
            "webapp-20240315-0900  # 正确格式",
            "myapp  # 错误：缺少时间戳",
            "myapp-2024-03-15  # 错误：日期格式不正确"
        });
    }

    #endregion
}