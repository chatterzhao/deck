using System.Diagnostics;
using Deck.Core.Interfaces;
using Deck.Core.Models;
using Microsoft.Extensions.Logging;

namespace Deck.Services;

/// <summary>
/// 远程模板服务实现 - 简化版Git基础同步功能
/// 基于deck-shell的remote-templates.sh，专注于MVP核心功能
/// </summary>
public class RemoteTemplatesService : IRemoteTemplatesService
{
    private readonly ILogger<RemoteTemplatesService> _logger;
    private readonly IConfigurationService _configurationService;
    private readonly INetworkService _networkService;

    // 常量定义
    private const string TemplatesDir = ".deck/templates";
    private const string DefaultTemplateRepo = "https://gitee.com/zhaoquan/deck.git";
    
    public RemoteTemplatesService(
        ILogger<RemoteTemplatesService> logger,
        IConfigurationService configurationService,
        INetworkService networkService)
    {
        _logger = logger;
        _configurationService = configurationService;
        _networkService = networkService;
    }

    public async Task<SyncResult> SyncTemplatesAsync(bool forceUpdate = false)
    {
        var result = new SyncResult();
        
        try
        {
            // 获取配置
            var config = await _configurationService.GetConfigAsync();
            var repoUrl = config?.RemoteTemplates?.Repository ?? DefaultTemplateRepo;
            
            if (string.IsNullOrEmpty(repoUrl))
            {
                result.Success = false;
                result.SyncLogs.Add("远程模板仓库未配置");
                return result;
            }
            
            // 检查网络连接
            var networkStatus = await _networkService.GetNetworkStatusAsync();
            if (!networkStatus.IsInternetAvailable)
            {
                result.Success = false;
                result.SyncLogs.Add("网络连接不可用，无法同步模板");
                return result;
            }
            
            // 确保目录存在
            Directory.CreateDirectory(TemplatesDir);
            
            // 执行Git同步
            result = await PerformGitSyncAsync(repoUrl, forceUpdate);
                
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "模板同步过程中发生错误");
            result.Success = false;
            result.SyncLogs.Add($"同步失败: {ex.Message}");
            return result;
        }
    }

    public async Task<List<TemplateInfo>> GetTemplateListAsync()
    {
        var templates = new List<TemplateInfo>();
        
        try
        {
            if (!Directory.Exists(TemplatesDir))
            {
                return templates;
            }
            
            var templateDirs = Directory.GetDirectories(TemplatesDir);
            foreach (var templateDir in templateDirs)
            {
                var templateName = Path.GetFileName(templateDir);
                var readmePath = Path.Combine(templateDir, "README.md");
                var composePath = Path.Combine(templateDir, "compose.yaml");
                var dockerfilePath = Path.Combine(templateDir, "Dockerfile");
                
                var templateInfo = new TemplateInfo
                {
                    Name = templateName,
                    Path = templateDir,
                    Description = File.Exists(readmePath) ? await ReadFirstLineAsync(readmePath) ?? string.Empty : string.Empty,
                    LastUpdated = Directory.GetLastWriteTime(templateDir)
                };
                
                templates.Add(templateInfo);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取模板列表时发生错误");
        }
        
        return templates;
    }

    public async Task<UpdateCheckResult> CheckTemplateUpdatesAsync()
    {
        // 简化实现：总是建议更新
        await Task.CompletedTask;
        return new UpdateCheckResult
        {
            HasUpdates = true
        };
    }

    public async Task<TemplateInfo?> GetTemplateInfoAsync(string templateName)
    {
        try
        {
            var templatePath = Path.Combine(TemplatesDir, templateName);
            if (!Directory.Exists(templatePath))
            {
                return null;
            }
            
            var readmePath = Path.Combine(templatePath, "README.md");
            var composePath = Path.Combine(templatePath, "compose.yaml");
            var dockerfilePath = Path.Combine(templatePath, "Dockerfile");
            
            return new TemplateInfo
            {
                Name = templateName,
                Path = templatePath,
                Description = File.Exists(readmePath) ? await ReadFirstLineAsync(readmePath) ?? string.Empty : string.Empty,
                LastUpdated = Directory.GetLastWriteTime(templatePath)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取模板信息时发生错误: {TemplateName}", templateName);
            return null;
        }
    }

    public async Task<TemplateValidationResult> ValidateTemplateAsync(string templateName)
    {
        var result = new TemplateValidationResult();
        
        try
        {
            var templatePath = Path.Combine(TemplatesDir, templateName);
            if (!Directory.Exists(templatePath))
            {
                result.IsValid = false;
                result.Errors.Add("模板目录不存在");
                return result;
            }
            
            // 检查必需文件
            var requiredFiles = new[] { "compose.yaml", "Dockerfile" };
            foreach (var file in requiredFiles)
            {
                var filePath = Path.Combine(templatePath, file);
                if (!File.Exists(filePath))
                {
                    result.IsValid = false;
                    result.Errors.Add($"缺少必需文件: {file}");
                }
            }
            
            result.IsValid = !result.Errors.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证模板时发生错误: {TemplateName}", templateName);
            result.IsValid = false;
            result.Errors.Add($"验证失败: {ex.Message}");
        }
        
        return await Task.FromResult(result);
    }

    public Task<bool> ClearTemplateCacheAsync()
    {
        try
        {
            if (Directory.Exists(TemplatesDir))
            {
                // 清空模板目录但保留目录本身
                var entries = Directory.GetFileSystemEntries(TemplatesDir);
                foreach (var entry in entries)
                {
                    if (Directory.Exists(entry))
                    {
                        Directory.Delete(entry, recursive: true);
                    }
                    else
                    {
                        File.Delete(entry);
                    }
                }
                
                _logger.LogInformation("模板缓存已清理");
                return Task.FromResult(true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理模板缓存时发生错误");
        }
        
        return Task.FromResult(false);
    }

    /// <summary>
    /// 执行Git同步操作 - 简化版实现
    /// </summary>
    private async Task<SyncResult> PerformGitSyncAsync(string repoUrl, bool forceUpdate)
    {
        var result = new SyncResult();
        var tempDir = Path.Combine(Path.GetTempPath(), $"deck-templates-{Guid.NewGuid():N}");
        
        try
        {
            Directory.CreateDirectory(tempDir);
            _logger.LogDebug("使用临时目录: {TempDir}", tempDir);
            
            // Git clone with sparse checkout for templates directory only
            var cloneResult = await RunGitCommandAsync(
                $"clone --no-checkout --filter=blob:none {repoUrl} {tempDir}");
                
            if (!cloneResult.IsSuccess)
            {
                result.Success = false;
                result.SyncLogs.Add($"Git克隆失败: {cloneResult.ErrorMessage}");
                return result;
            }
            
            // Configure sparse checkout for templates directory
            var sparseResult = await RunGitCommandAsync(
                "sparse-checkout init --cone", tempDir);
                
            if (sparseResult.IsSuccess)
            {
                await RunGitCommandAsync("sparse-checkout set templates", tempDir);
            }
            
            // Checkout main or master branch
            var checkoutResult = await RunGitCommandAsync("checkout main", tempDir);
            if (!checkoutResult.IsSuccess)
            {
                checkoutResult = await RunGitCommandAsync("checkout master", tempDir);
            }
            
            if (!checkoutResult.IsSuccess)
            {
                result.Success = false;
                result.SyncLogs.Add("无法检出代码分支");
                return result;
            }
            
            // Validate and copy templates
            var templatesSourceDir = Path.Combine(tempDir, "templates");
            if (!Directory.Exists(templatesSourceDir))
            {
                result.Success = false;
                result.SyncLogs.Add("远程仓库没有templates目录");
                return result;
            }
            
            // Clear existing templates if force update
            if (forceUpdate && Directory.Exists(TemplatesDir))
            {
                await ClearTemplateCacheAsync();
            }
            
            // Copy templates to project directory
            var copiedCount = await CopyTemplatesAsync(templatesSourceDir, TemplatesDir);
            
            result.Success = true;
            result.SyncedTemplateCount = copiedCount;
            result.NewTemplates = Directory.GetDirectories(TemplatesDir)
                .Select(dir => Path.GetFileName(dir))
                .ToList();
            result.SyncLogs.Add($"成功同步 {copiedCount} 个模板");
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Git同步操作失败");
            result.Success = false;
            result.SyncLogs.Add($"同步异常: {ex.Message}");
            return result;
        }
        finally
        {
            // Clean up temporary directory
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "清理临时目录失败: {TempDir}", tempDir);
            }
        }
    }

    /// <summary>
    /// 运行Git命令
    /// </summary>
    private async Task<GitOperationResult> RunGitCommandAsync(string arguments, string? workingDirectory = null)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            _logger.LogDebug("执行Git命令: git {Arguments} (工作目录: {WorkingDirectory})", 
                arguments, workingDirectory ?? Environment.CurrentDirectory);

            process.Start();
            
            var stdout = await process.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();
            
            await process.WaitForExitAsync();

            var isSuccess = process.ExitCode == 0;
            _logger.LogDebug("Git命令结果: 退出码={ExitCode}, 成功={Success}", process.ExitCode, isSuccess);

            return new GitOperationResult
            {
                IsSuccess = isSuccess,
                Output = stdout,
                ErrorMessage = stderr,
                ExitCode = process.ExitCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "运行Git命令时发生异常: git {Arguments}", arguments);
            return new GitOperationResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                ExitCode = -1
            };
        }
    }

    /// <summary>
    /// 复制模板文件
    /// </summary>
    private async Task<int> CopyTemplatesAsync(string sourceDir, string targetDir)
    {
        var copiedCount = 0;
        
        try
        {
            Directory.CreateDirectory(targetDir);
            
            var templateDirs = Directory.GetDirectories(sourceDir);
            foreach (var templateDir in templateDirs)
            {
                var templateName = Path.GetFileName(templateDir);
                var targetPath = Path.Combine(targetDir, templateName);
                
                // Remove existing template directory
                if (Directory.Exists(targetPath))
                {
                    Directory.Delete(targetPath, recursive: true);
                }
                
                // Copy template directory
                await CopyDirectoryAsync(templateDir, targetPath);
                copiedCount++;
                
                _logger.LogDebug("已复制模板: {TemplateName}", templateName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "复制模板时发生错误");
            throw;
        }
        
        return copiedCount;
    }

    /// <summary>
    /// 递归复制目录
    /// </summary>
    private async Task CopyDirectoryAsync(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);
        
        // Copy files
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            var targetFile = Path.Combine(targetDir, fileName);
            File.Copy(file, targetFile, overwrite: true);
        }
        
        // Copy subdirectories
        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(dir);
            var targetSubDir = Path.Combine(targetDir, dirName);
            await CopyDirectoryAsync(dir, targetSubDir);
        }
    }

    /// <summary>
    /// 读取文件的第一行作为描述
    /// </summary>
    private async Task<string?> ReadFirstLineAsync(string filePath)
    {
        try
        {
            using var reader = new StreamReader(filePath);
            var firstLine = await reader.ReadLineAsync();
            return firstLine?.TrimStart('#', ' ');
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Git操作结果
/// </summary>
internal class GitOperationResult
{
    public bool IsSuccess { get; set; }
    public string Output { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public int ExitCode { get; set; }
}