namespace Deck.Core.Interfaces;

/// <summary>
/// 文件系统操作服务接口
/// </summary>
public interface IFileSystemService
{
    /// <summary>
    /// 确保目录存在，不存在则创建
    /// </summary>
    /// <param name="directoryPath">目录路径</param>
    /// <returns>目录是否成功创建或已存在</returns>
    Task<bool> EnsureDirectoryExistsAsync(string directoryPath);
    
    /// <summary>
    /// 安全删除目录
    /// </summary>
    /// <param name="directoryPath">目录路径</param>
    /// <param name="recursive">是否递归删除</param>
    /// <returns>是否删除成功</returns>
    Task<bool> SafeDeleteDirectoryAsync(string directoryPath, bool recursive = true);
    
    /// <summary>
    /// 复制目录（包括隐藏文件）
    /// </summary>
    /// <param name="sourceDirectory">源目录</param>
    /// <param name="targetDirectory">目标目录</param>
    /// <param name="includeHiddenFiles">是否包括隐藏文件</param>
    /// <returns>复制是否成功</returns>
    Task<bool> CopyDirectoryAsync(string sourceDirectory, string targetDirectory, bool includeHiddenFiles = true);
    
    /// <summary>
    /// 获取目录大小（字节）
    /// </summary>
    /// <param name="directoryPath">目录路径</param>
    /// <returns>目录大小，失败返回-1</returns>
    Task<long> GetDirectorySizeAsync(string directoryPath);
    
    /// <summary>
    /// 检查文件是否存在
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>文件是否存在</returns>
    bool FileExists(string filePath);
    
    /// <summary>
    /// 检查目录是否存在
    /// </summary>
    /// <param name="directoryPath">目录路径</param>
    /// <returns>目录是否存在</returns>
    bool DirectoryExists(string directoryPath);
    
    /// <summary>
    /// 读取文本文件内容
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>文件内容，失败返回null</returns>
    Task<string?> ReadTextFileAsync(string filePath);
    
    /// <summary>
    /// 写入文本到文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="content">内容</param>
    /// <returns>写入是否成功</returns>
    Task<bool> WriteTextFileAsync(string filePath, string content);
}