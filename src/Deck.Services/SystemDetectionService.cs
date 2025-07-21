using Deck.Core.Interfaces;
using Deck.Core.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Deck.Services;

/// <summary>
/// 系统检测服务 - 基于 deck-shell 的 system-detect.sh 实现
/// 检测操作系统、架构、容器引擎、项目类型等系统信息
/// </summary>
public class SystemDetectionService : ISystemDetectionService
{
    private readonly ILogger<SystemDetectionService> _logger;

    public SystemDetectionService(ILogger<SystemDetectionService> logger)
    {
        _logger = logger;
    }

    public async Task<SystemInfo> GetSystemInfoAsync()
    {
        _logger.LogDebug("开始检测系统信息");

        var systemInfo = new SystemInfo
        {
            OperatingSystem = GetOperatingSystemType(),
            Architecture = GetSystemArchitecture(),
            Version = GetSystemVersion(),
            IsWsl = await IsWslEnvironmentAsync(),
            AvailableMemoryMb = GetAvailableMemoryMb(),
            AvailableDiskSpaceGb = GetAvailableDiskSpaceGb()
        };

        _logger.LogInformation("系统检测完成: {OS} {Arch} {Version}, WSL: {IsWsl}", 
            systemInfo.OperatingSystem, systemInfo.Architecture, systemInfo.Version, systemInfo.IsWsl);

        return systemInfo;
    }

    public async Task<ContainerEngineInfo> DetectContainerEngineAsync()
    {
        _logger.LogDebug("开始检测容器引擎");

        // 优先检测 Podman (对应 deck-shell 的优先级)
        var podmanInfo = await CheckContainerEngineAsync(ContainerEngineType.Podman, "podman");
        if (podmanInfo.IsAvailable)
        {
            return podmanInfo;
        }

        // 检测 Docker 作为备用
        var dockerInfo = await CheckContainerEngineAsync(ContainerEngineType.Docker, "docker");
        if (dockerInfo.IsAvailable)
        {
            return dockerInfo;
        }

        // 都不可用
        return new ContainerEngineInfo
        {
            Type = ContainerEngineType.None,
            IsAvailable = false,
            ErrorMessage = "未检测到可用的容器引擎 (Podman 或 Docker)"
        };
    }

    public async Task<ProjectTypeInfo> DetectProjectTypeAsync(string projectPath)
    {
        _logger.LogDebug("检测项目类型: {ProjectPath}", projectPath);

        var detectedTypes = new List<ProjectType>();
        ProjectType? recommendedType = null;

        // 基于 deck-shell 的项目类型检测逻辑
        var detectionRules = new Dictionary<ProjectType, Func<string, bool>>
        {
            [ProjectType.Tauri] = path => 
                File.Exists(Path.Combine(path, "tauri.conf.json")) || 
                File.Exists(Path.Combine(path, "src-tauri", "tauri.conf.json")),
            
            [ProjectType.Flutter] = path => 
                File.Exists(Path.Combine(path, "pubspec.yaml")) && 
                Directory.Exists(Path.Combine(path, "lib")),
            
            [ProjectType.Avalonia] = path => 
                Directory.GetFiles(path, "*.csproj", SearchOption.TopDirectoryOnly)
                    .Any(f => File.ReadAllText(f).Contains("Avalonia")),
            
            [ProjectType.DotNet] = path => 
                Directory.GetFiles(path, "*.csproj", SearchOption.TopDirectoryOnly).Length > 0 ||
                Directory.GetFiles(path, "*.sln", SearchOption.TopDirectoryOnly).Length > 0,
            
            [ProjectType.Python] = path => 
                File.Exists(Path.Combine(path, "requirements.txt")) ||
                File.Exists(Path.Combine(path, "pyproject.toml")) ||
                Directory.GetFiles(path, "*.py", SearchOption.TopDirectoryOnly).Length > 0,
            
            [ProjectType.Node] = path => 
                File.Exists(Path.Combine(path, "package.json"))
        };

        foreach (var rule in detectionRules)
        {
            try
            {
                if (rule.Value(projectPath))
                {
                    detectedTypes.Add(rule.Key);
                    _logger.LogDebug("检测到项目类型: {ProjectType}", rule.Key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("检测项目类型 {ProjectType} 时出错: {Error}", rule.Key, ex.Message);
            }
        }

        // 确定推荐类型（按优先级）
        var priorityOrder = new[] 
        { 
            ProjectType.Tauri, 
            ProjectType.Flutter, 
            ProjectType.Avalonia, 
            ProjectType.DotNet, 
            ProjectType.Python, 
            ProjectType.Node 
        };

        recommendedType = priorityOrder.FirstOrDefault(detectedTypes.Contains);

        return await Task.FromResult(new ProjectTypeInfo
        {
            DetectedTypes = detectedTypes,
            RecommendedType = recommendedType,
            ProjectRoot = projectPath
        });
    }

    public async Task<SystemRequirementsResult> CheckSystemRequirementsAsync()
    {
        _logger.LogDebug("检查系统要求");

        var checks = new List<RequirementCheck>();
        var warnings = new List<string>();

        // 内存检查 (最少 2GB)
        var memoryMb = GetAvailableMemoryMb();
        checks.Add(new RequirementCheck
        {
            Name = "可用内存",
            Passed = memoryMb >= 2048,
            Description = $"当前可用内存: {memoryMb}MB",
            Suggestion = memoryMb < 2048 ? "建议至少有 2GB 可用内存用于容器运行" : null
        });

        // 磁盘空间检查 (最少 5GB)
        var diskSpaceGb = GetAvailableDiskSpaceGb();
        checks.Add(new RequirementCheck
        {
            Name = "可用磁盘空间",
            Passed = diskSpaceGb >= 5,
            Description = $"当前可用磁盘空间: {diskSpaceGb}GB",
            Suggestion = diskSpaceGb < 5 ? "建议至少有 5GB 可用磁盘空间用于镜像存储" : null
        });

        // 容器引擎检查
        var containerEngine = await DetectContainerEngineAsync();
        checks.Add(new RequirementCheck
        {
            Name = "容器引擎",
            Passed = containerEngine.IsAvailable,
            Description = containerEngine.IsAvailable 
                ? $"检测到 {containerEngine.Type} {containerEngine.Version}" 
                : "未检测到可用的容器引擎",
            Suggestion = !containerEngine.IsAvailable ? "请安装 Podman 或 Docker" : null
        });

        var allPassed = checks.All(c => c.Passed);

        return new SystemRequirementsResult
        {
            MeetsRequirements = allPassed,
            Checks = checks,
            Warnings = warnings
        };
    }

    #region Private Methods

    private static OperatingSystemType GetOperatingSystemType()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return OperatingSystemType.Windows;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return OperatingSystemType.Linux;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return OperatingSystemType.MacOS;
        
        return OperatingSystemType.Unknown;
    }

    private static SystemArchitecture GetSystemArchitecture()
    {
        return RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => SystemArchitecture.X64,
            Architecture.Arm64 => SystemArchitecture.ARM64,
            Architecture.X86 => SystemArchitecture.X86,
            _ => SystemArchitecture.Unknown
        };
    }

    private static string GetSystemVersion()
    {
        return RuntimeInformation.OSDescription;
    }

    private async Task<bool> IsWslEnvironmentAsync()
    {
        try
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return false;

            // 检查 /proc/version 是否包含 Microsoft 或 WSL
            if (File.Exists("/proc/version"))
            {
                var version = await File.ReadAllTextAsync("/proc/version");
                return version.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) ||
                       version.Contains("WSL", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static long GetAvailableMemoryMb()
    {
        try
        {
            var info = GC.GetGCMemoryInfo();
            return info.TotalAvailableMemoryBytes / (1024 * 1024);
        }
        catch
        {
            return 0;
        }
    }

    private static long GetAvailableDiskSpaceGb()
    {
        try
        {
            var drive = new DriveInfo(Directory.GetCurrentDirectory());
            return drive.AvailableFreeSpace / (1024 * 1024 * 1024);
        }
        catch
        {
            return 0;
        }
    }

    private async Task<ContainerEngineInfo> CheckContainerEngineAsync(ContainerEngineType type, string command)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                return new ContainerEngineInfo
                {
                    Type = type,
                    Version = output.Trim().Split('\n')[0], // 取第一行作为版本信息
                    IsAvailable = true,
                    InstallPath = command
                };
            }

            return new ContainerEngineInfo
            {
                Type = type,
                IsAvailable = false,
                ErrorMessage = !string.IsNullOrWhiteSpace(error) ? error : "命令执行失败"
            };
        }
        catch (Exception ex)
        {
            return new ContainerEngineInfo
            {
                Type = type,
                IsAvailable = false,
                ErrorMessage = ex.Message
            };
        }
    }

    #endregion
}