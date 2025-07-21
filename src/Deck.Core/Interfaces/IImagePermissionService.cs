using Deck.Core.Models;

namespace Deck.Core.Interfaces;

/// <summary>
/// 镜像权限管理服务接口 - 对应 deck-shell 的 directory-mgmt.sh 权限管理功能
/// 提供镜像目录和文件的权限验证，防止未授权的修改操作
/// </summary>
public interface IImagePermissionService
{
    /// <summary>
    /// 验证文件操作权限
    /// </summary>
    /// <param name="imagePath">镜像目录路径</param>
    /// <param name="filePath">文件路径（相对于镜像目录）</param>
    /// <param name="operation">操作类型</param>
    Task<FilePermissionResult> ValidateFilePermissionAsync(string imagePath, string filePath, FileOperation operation);

    /// <summary>
    /// 验证目录操作权限
    /// </summary>
    /// <param name="imagePath">镜像目录路径</param>
    /// <param name="operation">操作类型</param>
    Task<DirectoryPermissionResult> ValidateDirectoryOperationAsync(string imagePath, DirectoryOperation operation);

    /// <summary>
    /// 验证环境变量修改权限
    /// </summary>
    /// <param name="imagePath">镜像目录路径</param>
    /// <param name="envChanges">环境变量变更（变量名 -> 新值）</param>
    Task<EnvPermissionResult> ValidateEnvFileChangesAsync(string imagePath, Dictionary<string, string?> envChanges);

    /// <summary>
    /// 获取镜像目录的权限概况
    /// </summary>
    /// <param name="imagePath">镜像目录路径</param>
    Task<ImagePermissionSummary> GetImagePermissionSummaryAsync(string imagePath);

    /// <summary>
    /// 验证镜像目录名称是否符合规范
    /// </summary>
    /// <param name="directoryName">目录名称</param>
    Task<DirectoryNameValidationResult> ValidateImageDirectoryNameAsync(string directoryName);

    /// <summary>
    /// 获取运行时可修改的环境变量列表
    /// </summary>
    Task<List<string>> GetRuntimeModifiableVariablesAsync();

    /// <summary>
    /// 检查是否为受保护的配置文件
    /// </summary>
    /// <param name="fileName">文件名</param>
    Task<bool> IsProtectedConfigFileAsync(string fileName);

    /// <summary>
    /// 获取权限违规的详细说明和修复建议
    /// </summary>
    /// <param name="violation">权限违规信息</param>
    Task<PermissionGuidance> GetPermissionGuidanceAsync(PermissionViolation violation);
}