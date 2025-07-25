using System.Collections.Generic;

namespace Deck.Core.Models;

/// <summary>
/// 文件操作结果
/// </summary>
public class FileOperationResult
{
    /// <summary>
    /// 文件路径
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 文件内容是否发生变化
    /// </summary>
    public bool Changed { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 目录操作结果
/// </summary>
public class DirectoryOperationResult
{
    /// <summary>
    /// 目录路径
    /// </summary>
    public string DirectoryPath { get; set; } = string.Empty;

    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 文件操作结果列表
    /// </summary>
    public List<FileOperationResult> FileResults { get; set; } = new();
}