using Deck.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Deck.Services;

/// <summary>
/// 文件系统操作服务实现
/// </summary>
public class FileSystemService : IFileSystemService
{
    private readonly ILogger<FileSystemService> _logger;

    public FileSystemService(ILogger<FileSystemService> logger)
    {
        _logger = logger;
    }

    public Task<bool> EnsureDirectoryExistsAsync(string directoryPath)
    {
        try
        {
            if (string.IsNullOrEmpty(directoryPath))
            {
                return Task.FromResult(false);
            }

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                _logger.LogDebug("创建目录: {DirectoryPath}", directoryPath);
            }

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建目录失败: {DirectoryPath}", directoryPath);
            return Task.FromResult(false);
        }
    }

    public Task<bool> SafeDeleteDirectoryAsync(string directoryPath, bool recursive = true)
    {
        try
        {
            if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
            {
                return Task.FromResult(true);
            }

            Directory.Delete(directoryPath, recursive);
            _logger.LogDebug("删除目录: {DirectoryPath}", directoryPath);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除目录失败: {DirectoryPath}", directoryPath);
            return Task.FromResult(false);
        }
    }

    public async Task<bool> CopyDirectoryAsync(string sourceDirectory, string targetDirectory, bool includeHiddenFiles = true)
    {
        try
        {
            if (!Directory.Exists(sourceDirectory))
            {
                _logger.LogWarning("源目录不存在: {SourceDirectory}", sourceDirectory);
                return false;
            }

            await EnsureDirectoryExistsAsync(targetDirectory);

            var sourceDir = new DirectoryInfo(sourceDirectory);
            
            // 复制文件
            foreach (var file in sourceDir.GetFiles("*", SearchOption.TopDirectoryOnly))
            {
                // 跳过隐藏文件（如果设置为false）
                if (!includeHiddenFiles && file.Attributes.HasFlag(FileAttributes.Hidden))
                {
                    continue;
                }

                var targetPath = Path.Combine(targetDirectory, file.Name);
                file.CopyTo(targetPath, true);
            }

            // 递归复制子目录
            foreach (var subDir in sourceDir.GetDirectories())
            {
                // 跳过隐藏目录（如果设置为false）
                if (!includeHiddenFiles && subDir.Attributes.HasFlag(FileAttributes.Hidden))
                {
                    continue;
                }

                var targetSubDir = Path.Combine(targetDirectory, subDir.Name);
                await CopyDirectoryAsync(subDir.FullName, targetSubDir, includeHiddenFiles);
            }

            _logger.LogDebug("复制目录完成: {SourceDirectory} -> {TargetDirectory}", sourceDirectory, targetDirectory);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "复制目录失败: {SourceDirectory} -> {TargetDirectory}", sourceDirectory, targetDirectory);
            return false;
        }
    }

    public async Task<long> GetDirectorySizeAsync(string directoryPath)
    {
        try
        {
            if (!Directory.Exists(directoryPath))
            {
                return -1;
            }

            var dirInfo = new DirectoryInfo(directoryPath);
            return await Task.Run(() => 
            {
                return dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                    .Sum(file => file.Length);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取目录大小失败: {DirectoryPath}", directoryPath);
            return -1;
        }
    }

    public bool FileExists(string filePath)
    {
        return !string.IsNullOrEmpty(filePath) && File.Exists(filePath);
    }

    public bool DirectoryExists(string directoryPath)
    {
        return !string.IsNullOrEmpty(directoryPath) && Directory.Exists(directoryPath);
    }

    public async Task<string?> ReadTextFileAsync(string filePath)
    {
        try
        {
            if (!FileExists(filePath))
            {
                return null;
            }

            return await File.ReadAllTextAsync(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取文件失败: {FilePath}", filePath);
            return null;
        }
    }

    public async Task<bool> WriteTextFileAsync(string filePath, string content)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                await EnsureDirectoryExistsAsync(directory);
            }

            await File.WriteAllTextAsync(filePath, content);
            _logger.LogDebug("写入文件: {FilePath}", filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "写入文件失败: {FilePath}", filePath);
            return false;
        }
    }
}