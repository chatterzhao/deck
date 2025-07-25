using System.Collections.Generic;
using System.Threading.Tasks;
using Deck.Core.Models;

namespace Deck.Core.Interfaces;

/// <summary>
/// 模板变量引擎接口
/// </summary>
public interface ITemplateVariableEngine
{
    /// <summary>
    /// 替换文本中的模板变量
    /// </summary>
    /// <param name="content">原始内容</param>
    /// <param name="variables">变量字典</param>
    /// <returns>替换后的文本</returns>
    string ReplaceVariables(string content, IDictionary<string, string>? variables);

    /// <summary>
    /// 替换文件中的模板变量
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="variables">变量字典</param>
    /// <returns>操作结果</returns>
    Task<FileOperationResult> ReplaceVariablesInFileAsync(string filePath, IDictionary<string, string>? variables);

    /// <summary>
    /// 替换目录中所有文件的模板变量
    /// </summary>
    /// <param name="directoryPath">目录路径</param>
    /// <param name="variables">变量字典</param>
    /// <returns>操作结果</returns>
    Task<DirectoryOperationResult> ReplaceVariablesInDirectoryAsync(string directoryPath, IDictionary<string, string>? variables);
}