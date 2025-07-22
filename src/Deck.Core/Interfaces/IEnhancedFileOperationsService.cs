using Deck.Core.Models;

namespace Deck.Core.Interfaces;

/// <summary>
/// 增强文件操作服务接口
/// 负责处理标准端口管理、PROJECT_NAME更新、隐藏文件复制和文件操作验证回滚
/// </summary>
public interface IEnhancedFileOperationsService
{
    /// <summary>
    /// 处理标准端口配置
    /// 检测端口冲突并自动分配可用端口
    /// </summary>
    /// <param name="envFilePath">.env文件路径</param>
    /// <param name="options">操作选项</param>
    /// <returns>端口处理结果</returns>
    Task<StandardPortsResult> ProcessStandardPortsAsync(string envFilePath, EnhancedFileOperationOptions? options = null);

    /// <summary>
    /// 更新PROJECT_NAME以避免容器名冲突
    /// </summary>
    /// <param name="envFilePath">.env文件路径</param>
    /// <param name="imageName">镜像名称（用作PROJECT_NAME）</param>
    /// <param name="options">操作选项</param>
    /// <returns>PROJECT_NAME更新结果</returns>
    Task<ProjectNameUpdateResult> UpdateProjectNameAsync(string envFilePath, string imageName, EnhancedFileOperationOptions? options = null);

    /// <summary>
    /// 复制文件（包含隐藏文件）
    /// 确保.env、.gitignore等隐藏文件也被复制
    /// </summary>
    /// <param name="sourceDirectory">源目录</param>
    /// <param name="targetDirectory">目标目录</param>
    /// <param name="options">操作选项</param>
    /// <returns>文件复制结果</returns>
    Task<FileCopyResult> CopyWithHiddenFilesAsync(string sourceDirectory, string targetDirectory, EnhancedFileOperationOptions? options = null);

    /// <summary>
    /// 验证配置文件并创建备份
    /// </summary>
    /// <param name="configFilePath">配置文件路径</param>
    /// <param name="options">操作选项</param>
    /// <returns>备份结果</returns>
    Task<ConfigBackupResult> ValidateAndBackupConfigAsync(string configFilePath, EnhancedFileOperationOptions? options = null);

    /// <summary>
    /// 从备份恢复配置文件
    /// </summary>
    /// <param name="backupFilePath">备份文件路径</param>
    /// <param name="targetFilePath">目标文件路径</param>
    /// <returns>恢复结果</returns>
    Task<ConfigRestoreResult> RestoreConfigFromBackupAsync(string backupFilePath, string targetFilePath);

    /// <summary>
    /// 获取.env文件中的标准端口配置
    /// </summary>
    /// <param name="envFilePath">.env文件路径</param>
    /// <returns>端口配置字典</returns>
    Task<Dictionary<string, int>> GetStandardPortsAsync(string envFilePath);

    /// <summary>
    /// 验证.env文件格式
    /// </summary>
    /// <param name="envFilePath">.env文件路径</param>
    /// <returns>验证结果</returns>
    Task<EnvFileValidationResult> ValidateEnvFileFormatAsync(string envFilePath);

    /// <summary>
    /// 清理过期的备份文件
    /// </summary>
    /// <param name="backupDirectory">备份目录</param>
    /// <param name="retentionDays">保留天数</param>
    /// <returns>清理的文件数量</returns>
    Task<int> CleanupExpiredBackupsAsync(string backupDirectory, int retentionDays = 30);

    /// <summary>
    /// 获取文件操作历史记录
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="operationType">操作类型过滤</param>
    /// <returns>操作记录列表</returns>
    Task<List<FileOperationRecord>> GetFileOperationHistoryAsync(string filePath, FileOperationType? operationType = null);
}