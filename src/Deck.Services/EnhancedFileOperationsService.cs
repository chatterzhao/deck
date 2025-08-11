using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Deck.Core.Interfaces;
using Deck.Core.Models;

namespace Deck.Services;

/// <summary>
/// 增强文件操作服务实现
/// </summary>
public class EnhancedFileOperationsService : IEnhancedFileOperationsService
{
    private readonly ILogger<EnhancedFileOperationsService> _logger;
    private readonly IPortConflictService _portConflictService;
    private readonly ITemplateVariableEngine _templateVariableEngine;
    private static readonly Regex EnvLineRegex = new(@"^\s*([^=\s]+)\s*=\s*(.*)$", RegexOptions.Compiled);
    private static readonly Regex CommentOrEmptyLineRegex = new(@"^\s*(#.*)?$", RegexOptions.Compiled);

    public EnhancedFileOperationsService(
        ILogger<EnhancedFileOperationsService> logger,
        IPortConflictService portConflictService,
        ITemplateVariableEngine templateVariableEngine)
    {
        _logger = logger;
        _portConflictService = portConflictService;
        _templateVariableEngine = templateVariableEngine;
    }

    public async Task<StandardPortsResult> ProcessStandardPortsAsync(string envFilePath, EnhancedFileOperationOptions? options = null)
    {
        options ??= new EnhancedFileOperationOptions();
        var result = new StandardPortsResult();

        try
        {
            // 只在需要时记录日志
            if (options.CreateBackup)
            {
                _logger.LogInformation("开始处理标准端口配置: {EnvFilePath}", envFilePath);
            }

            // 读取当前端口配置
            var currentPorts = await GetStandardPortsAsync(envFilePath);
            result.AllPorts = new Dictionary<string, int>(currentPorts);

            // 确保所有标准端口都存在
            foreach (var (portVar, defaultPort) in StandardPorts.DefaultPorts)
            {
                if (!currentPorts.ContainsKey(portVar))
                {
                    currentPorts[portVar] = defaultPort;
                    if (options.CreateBackup)
                    {
                        _logger.LogInformation("添加缺失的端口变量: {PortVar}={DefaultPort}", portVar, defaultPort);
                    }
                }
            }

            // 检测端口冲突
            var portValues = currentPorts.Values.ToList();
            var conflictResults = await _portConflictService.CheckPortsAsync(portValues);

            // 处理端口冲突 - 确保每个端口都分配到不同的值
            var usedPorts = new HashSet<int>(currentPorts.Values.Where(p => !conflictResults.Any(c => c.Port == p && !c.IsAvailable)));
            
            foreach (var conflict in conflictResults.Where(c => !c.IsAvailable))
            {
                var portVar = currentPorts.FirstOrDefault(p => p.Value == conflict.Port).Key;
                if (!string.IsNullOrEmpty(portVar))
                {
                    var availablePort = await FindNextAvailablePortAsync(
                        conflict.Port,
                        options.PortRangeStart,
                        options.PortRangeEnd,
                        usedPorts);

                    if (availablePort.HasValue && availablePort != conflict.Port)
                    {
                        result.ModifiedPorts[portVar] = availablePort.Value;
                        currentPorts[portVar] = availablePort.Value;
                        usedPorts.Add(availablePort.Value); // 记录已使用的端口
                        result.Warnings.Add($"端口冲突：{portVar} 从 {conflict.Port} 更改为 {availablePort.Value}");
                        if (options.CreateBackup)
                        {
                            _logger.LogWarning("端口冲突解决: {PortVar} {OldPort} -> {NewPort}", portVar, conflict.Port, availablePort.Value);
                        }
                    }
                }
            }

            // 只有在需要创建备份时才更新.env文件
            if (options.CreateBackup)
            {
                await UpdateEnvFilePortsAsync(envFilePath, currentPorts, options);
                _logger.LogInformation("标准端口处理完成，修改了 {Count} 个端口", result.ModifiedPorts.Count);
            }

            result.AllPorts = currentPorts;
            result.IsSuccess = true;
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "处理标准端口时发生错误: {EnvFilePath}", envFilePath);
        }

        return result;
    }

    /// <summary>
    /// 查找下一个可用端口，避免与已使用的端口冲突
    /// </summary>
    private async Task<int?> FindNextAvailablePortAsync(int preferredPort, int startRange, int endRange, HashSet<int> usedPorts)
    {
        // 从首选端口开始搜索
        for (int port = Math.Max(preferredPort, startRange); port <= endRange; port++)
        {
            // 跳过已经被其他变量使用的端口
            if (usedPorts.Contains(port))
                continue;
                
            var availablePort = await _portConflictService.FindAvailablePortAsync(port, port, port);
            if (availablePort.HasValue)
            {
                return availablePort.Value;
            }
        }

        // 如果首选端口之后没有找到，从范围开始处搜索
        for (int port = startRange; port < Math.Max(preferredPort, startRange); port++)
        {
            if (usedPorts.Contains(port))
                continue;
                
            var availablePort = await _portConflictService.FindAvailablePortAsync(port, port, port);
            if (availablePort.HasValue)
            {
                return availablePort.Value;
            }
        }

        return null;
    }

    public async Task<ProjectNameUpdateResult> UpdateProjectNameAsync(string envFilePath, string imageName, EnhancedFileOperationOptions? options = null)
    {
        options ??= new EnhancedFileOperationOptions();
        var result = new ProjectNameUpdateResult();

        try
        {
            _logger.LogInformation("更新PROJECT_NAME: {EnvFilePath} -> {ImageName}", envFilePath, imageName);

            if (options.CreateBackup)
            {
                await ValidateAndBackupConfigAsync(envFilePath, options);
            }

            // 读取现有内容
            var lines = await File.ReadAllLinesAsync(envFilePath);
            var updatedLines = new List<string>();
            bool projectNameFound = false;

            foreach (var line in lines)
            {
                if (line.TrimStart().StartsWith("PROJECT_NAME="))
                {
                    var match = EnvLineRegex.Match(line);
                    if (match.Success)
                    {
                        result.PreviousProjectName = match.Groups[2].Value.Trim().Trim('"', '\'');
                        updatedLines.Add($"PROJECT_NAME={imageName}");
                        projectNameFound = true;
                        _logger.LogInformation("更新PROJECT_NAME: {Old} -> {New}", result.PreviousProjectName, imageName);
                    }
                    else
                    {
                        updatedLines.Add($"PROJECT_NAME={imageName}");
                        projectNameFound = true;
                    }
                }
                else
                {
                    updatedLines.Add(line);
                }
            }

            // 如果没有找到PROJECT_NAME，则添加
            if (!projectNameFound)
            {
                updatedLines.Add($"PROJECT_NAME={imageName}");
                _logger.LogInformation("添加PROJECT_NAME: {ImageName}", imageName);
            }

            // 写入更新后的内容
            await File.WriteAllLinesAsync(envFilePath, updatedLines);

            result.UpdatedProjectName = imageName;
            result.IsSuccess = true;
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "更新PROJECT_NAME时发生错误: {EnvFilePath}", envFilePath);
        }

        return result;
    }

    public async Task<FileCopyResult> CopyWithHiddenFilesAsync(string sourceDirectory, string targetDirectory, EnhancedFileOperationOptions? options = null)
    {
        options ??= new EnhancedFileOperationOptions();
        var result = new FileCopyResult();

        try
        {
            _logger.LogInformation("复制目录（包含隐藏文件）: {Source} -> {Target}", sourceDirectory, targetDirectory);

            if (!Directory.Exists(sourceDirectory))
            {
                throw new DirectoryNotFoundException($"源目录不存在: {sourceDirectory}");
            }

            Directory.CreateDirectory(targetDirectory);

            // 获取所有文件（包括隐藏文件）
            var allFiles = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories);

            foreach (var sourceFile in allFiles)
            {
                try
                {
                    var relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
                    var targetFile = Path.Combine(targetDirectory, relativePath);

                    // 确保目标目录存在
                    var targetDir = Path.GetDirectoryName(targetFile);
                    if (!string.IsNullOrEmpty(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    // 检查是否覆盖现有文件
                    if (File.Exists(targetFile) && !options.OverwriteExisting)
                    {
                        result.SkippedFiles.Add(relativePath);
                        _logger.LogInformation("跳过现有文件: {File}", relativePath);
                        continue;
                    }

                    File.Copy(sourceFile, targetFile, options.OverwriteExisting);
                    result.CopiedFiles.Add(relativePath);
                    _logger.LogDebug("复制文件: {File}", relativePath);
                }
                catch (Exception ex)
                {
                    var relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
                    result.FailedFiles.Add(relativePath);
                    _logger.LogWarning(ex, "复制文件失败: {File}", relativePath);
                }
            }

            // 如果提供了变量，则进行变量替换
            if (options.Variables != null && options.Variables.Count > 0)
            {
                _logger.LogInformation("开始替换模板变量，变量数量: {VariableCount}", options.Variables.Count);
                var replaceResult = await _templateVariableEngine.ReplaceVariablesInDirectoryAsync(targetDirectory, options.Variables);
                if (!replaceResult.IsSuccess)
                {
                    _logger.LogWarning("模板变量替换失败: {ErrorMessage}", replaceResult.ErrorMessage);
                }
                else
                {
                    var changedFiles = 0;
                    foreach (var fileResult in replaceResult.FileResults)
                    {
                        if (fileResult.Changed)
                        {
                            changedFiles++;
                        }
                    }
                    _logger.LogInformation("模板变量替换完成，共修改 {ChangedFileCount} 个文件", changedFiles);
                }
            }

            result.IsSuccess = true;
            _logger.LogInformation("目录复制完成，成功: {Success}, 跳过: {Skipped}, 失败: {Failed}",
                result.CopiedFiles.Count, result.SkippedFiles.Count, result.FailedFiles.Count);
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "复制目录时发生错误: {Source} -> {Target}", sourceDirectory, targetDirectory);
        }

        return result;
    }

    public async Task<ConfigBackupResult> ValidateAndBackupConfigAsync(string configFilePath, EnhancedFileOperationOptions? options = null)
    {
        options ??= new EnhancedFileOperationOptions();
        var result = new ConfigBackupResult();

        try
        {
            if (!File.Exists(configFilePath))
            {
                throw new FileNotFoundException($"配置文件不存在: {configFilePath}");
            }

            // 验证文件格式
            if (options.ValidateFormat && Path.GetExtension(configFilePath).Equals(".env", StringComparison.OrdinalIgnoreCase))
            {
                var validationResult = await ValidateEnvFileFormatAsync(configFilePath);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("配置文件格式验证失败: {Errors}", string.Join(", ", validationResult.Errors));
                }
            }

            // 创建备份
            var backupDir = Path.Combine(Path.GetDirectoryName(configFilePath)!, "backups");
            Directory.CreateDirectory(backupDir);

            var fileName = Path.GetFileName(configFilePath);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            var backupFileName = $"{fileName}.{timestamp}.bak";
            var backupFilePath = Path.Combine(backupDir, backupFileName);

            // 如果文件已存在，添加唯一后缀
            if (File.Exists(backupFilePath))
            {
                var counter = 1;
                var baseFileName = Path.GetFileNameWithoutExtension(backupFileName);
                var extension = Path.GetExtension(backupFileName);
                
                do
                {
                    backupFileName = $"{baseFileName}_{counter:D2}{extension}";
                    backupFilePath = Path.Combine(backupDir, backupFileName);
                    counter++;
                } while (File.Exists(backupFilePath) && counter < 100);
            }

            File.Copy(configFilePath, backupFilePath);

            result.BackupFilePath = backupFilePath;
            result.BackupTimestamp = DateTime.UtcNow;
            result.IsSuccess = true;

            _logger.LogInformation("配置文件备份完成: {BackupPath}", backupFilePath);
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "备份配置文件时发生错误: {ConfigFilePath}", configFilePath);
        }

        return result;
    }

    public Task<ConfigRestoreResult> RestoreConfigFromBackupAsync(string backupFilePath, string targetFilePath)
    {
        var result = new ConfigRestoreResult();

        try
        {
            if (!File.Exists(backupFilePath))
            {
                throw new FileNotFoundException($"备份文件不存在: {backupFilePath}");
            }

            // 确保目标目录存在
            var targetDir = Path.GetDirectoryName(targetFilePath);
            if (!string.IsNullOrEmpty(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            File.Copy(backupFilePath, targetFilePath, true);

            result.RestoredFilePath = targetFilePath;
            result.RestoreTimestamp = DateTime.UtcNow;
            result.IsSuccess = true;

            _logger.LogInformation("配置文件恢复完成: {BackupPath} -> {TargetPath}", backupFilePath, targetFilePath);
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "恢复配置文件时发生错误: {BackupPath} -> {TargetPath}", backupFilePath, targetFilePath);
        }

        return Task.FromResult(result);
    }

    public async Task<Dictionary<string, int>> GetStandardPortsAsync(string envFilePath)
    {
        var ports = new Dictionary<string, int>();

        if (!File.Exists(envFilePath))
        {
            _logger.LogWarning(".env文件不存在，返回默认端口配置: {EnvFilePath}", envFilePath);
            return new Dictionary<string, int>(StandardPorts.DefaultPorts);
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(envFilePath);

            foreach (var line in lines)
            {
                if (CommentOrEmptyLineRegex.IsMatch(line))
                    continue;

                var match = EnvLineRegex.Match(line);
                if (match.Success)
                {
                    var key = match.Groups[1].Value.Trim();
                    var value = match.Groups[2].Value.Trim().Trim('"', '\'');

                    if (StandardPorts.RequiredPortVariables.Contains(key) && int.TryParse(value, out var port))
                    {
                        ports[key] = port;
                    }
                }
            }

            // 添加缺失的默认端口
            foreach (var (portVar, defaultPort) in StandardPorts.DefaultPorts)
            {
                if (!ports.ContainsKey(portVar))
                {
                    ports[portVar] = defaultPort;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取.env文件时发生错误: {EnvFilePath}", envFilePath);
            return new Dictionary<string, int>(StandardPorts.DefaultPorts);
        }

        return ports;
    }

    public async Task<EnvFileValidationResult> ValidateEnvFileFormatAsync(string envFilePath)
    {
        var result = new EnvFileValidationResult { IsValid = true };

        if (!File.Exists(envFilePath))
        {
            result.IsValid = false;
            result.Errors.Add($"文件不存在: {envFilePath}");
            return result;
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(envFilePath);
            var lineNumber = 0;

            foreach (var line in lines)
            {
                lineNumber++;

                // 跳过注释和空行
                if (CommentOrEmptyLineRegex.IsMatch(line))
                    continue;

                // 检查格式
                if (!EnvLineRegex.IsMatch(line))
                {
                    result.IsValid = false;
                    result.Errors.Add($"第 {lineNumber} 行格式不正确，应该是 KEY=VALUE 格式: {line.Trim()}");
                    continue;
                }

                var match = EnvLineRegex.Match(line);
                var key = match.Groups[1].Value.Trim();
                var value = match.Groups[2].Value.Trim().Trim('"', '\'');

                // 检查重复键
                if (result.ParsedVariables.ContainsKey(key))
                {
                    result.Warnings.Add($"检测到重复的环境变量: {key} (第 {lineNumber} 行)");
                }
                else
                {
                    result.ParsedVariables[key] = value;
                }

                // 验证端口号
                if (StandardPorts.RequiredPortVariables.Contains(key))
                {
                    if (!int.TryParse(value, out var port) || port <= 0 || port > 65535)
                    {
                        result.IsValid = false;
                        result.Errors.Add($"无效的端口号: {key}={value} (第 {lineNumber} 行)");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add($"读取文件时发生错误: {ex.Message}");
            _logger.LogError(ex, "验证.env文件格式时发生错误: {EnvFilePath}", envFilePath);
        }

        return result;
    }

    public Task<int> CleanupExpiredBackupsAsync(string backupDirectory, int retentionDays = 30)
    {
        var deletedCount = 0;

        try
        {
            if (!Directory.Exists(backupDirectory))
                return Task.FromResult(0);

            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
            var backupFiles = Directory.GetFiles(backupDirectory, "*.bak", SearchOption.AllDirectories);

            foreach (var backupFile in backupFiles)
            {
                var fileInfo = new FileInfo(backupFile);
                if (fileInfo.CreationTimeUtc < cutoffDate)
                {
                    try
                    {
                        File.Delete(backupFile);
                        deletedCount++;
                        _logger.LogDebug("删除过期备份文件: {BackupFile}", backupFile);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "删除备份文件失败: {BackupFile}", backupFile);
                    }
                }
            }

            _logger.LogInformation("清理过期备份完成，删除了 {DeletedCount} 个文件", deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理过期备份时发生错误: {BackupDirectory}", backupDirectory);
        }

        return Task.FromResult(deletedCount);
    }

    public Task<List<FileOperationRecord>> GetFileOperationHistoryAsync(string filePath, FileOperationType? operationType = null)
    {
        // 这是一个简化实现，实际项目中可能需要持久化存储操作历史
        var records = new List<FileOperationRecord>();

        try
        {
            var backupDir = Path.Combine(Path.GetDirectoryName(filePath)!, "backups");
            if (!Directory.Exists(backupDir))
                return Task.FromResult(records);

            var fileName = Path.GetFileName(filePath);
            var backupFiles = Directory.GetFiles(backupDir, $"{fileName}.*.bak");

            foreach (var backupFile in backupFiles)
            {
                var fileInfo = new FileInfo(backupFile);
                var record = new FileOperationRecord
                {
                    Type = FileOperationType.Backup,
                    SourcePath = filePath,
                    TargetPath = backupFile,
                    Timestamp = fileInfo.CreationTimeUtc,
                    IsSuccess = true,
                    BackupPath = backupFile
                };

                if (operationType == null || record.Type == operationType)
                {
                    records.Add(record);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取文件操作历史时发生错误: {FilePath}", filePath);
        }

        return Task.FromResult(records.OrderByDescending(r => r.Timestamp).ToList());
    }

    private async Task UpdateEnvFilePortsAsync(string envFilePath, Dictionary<string, int> ports, EnhancedFileOperationOptions options)
    {
        if (options.CreateBackup)
        {
            await ValidateAndBackupConfigAsync(envFilePath, options);
        }

        var lines = File.Exists(envFilePath) ? await File.ReadAllLinesAsync(envFilePath) : new string[0];
        var updatedLines = new List<string>();
        var processedPorts = new HashSet<string>();

        // 更新现有行
        foreach (var line in lines)
        {
            if (CommentOrEmptyLineRegex.IsMatch(line))
            {
                updatedLines.Add(line);
                continue;
            }

            var match = EnvLineRegex.Match(line);
            if (match.Success)
            {
                var key = match.Groups[1].Value.Trim();
                if (ports.ContainsKey(key))
                {
                    updatedLines.Add($"{key}={ports[key]}");
                    processedPorts.Add(key);
                }
                else
                {
                    updatedLines.Add(line);
                }
            }
            else
            {
                updatedLines.Add(line);
            }
        }

        // 添加新的端口变量
        foreach (var (portVar, portValue) in ports)
        {
            if (!processedPorts.Contains(portVar))
            {
                updatedLines.Add($"{portVar}={portValue}");
            }
        }

        await File.WriteAllLinesAsync(envFilePath, updatedLines);
    }
}