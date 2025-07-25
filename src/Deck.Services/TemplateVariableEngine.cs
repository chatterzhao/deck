using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Deck.Core.Interfaces;
using Deck.Core.Models;
using Microsoft.Extensions.Logging;

namespace Deck.Services;

/// <summary>
/// 模板变量引擎实现
/// </summary>
public class TemplateVariableEngine : ITemplateVariableEngine
{
    private readonly ILogger<TemplateVariableEngine> _logger;
    private static readonly Regex VariableRegex = new(@"(\$\{([^}]+)\})|(\{\{([^}]+)\}\})", RegexOptions.Compiled);

    public TemplateVariableEngine(ILogger<TemplateVariableEngine> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public string ReplaceVariables(string content, IDictionary<string, string>? variables)
    {
        if (string.IsNullOrEmpty(content) || variables == null || variables.Count == 0)
        {
            return content;
        }

        return VariableRegex.Replace(content, match =>
        {
            // 匹配 ${VAR} 或 {{VAR}} 格式
            var variableName = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[4].Value;
            
            if (variables.TryGetValue(variableName, out var value))
            {
                _logger.LogDebug("替换变量 {VariableName} 为 {Value}", variableName, value);
                return value;
            }

            _logger.LogDebug("未找到变量 {VariableName} 的值，保持原样", variableName);
            return match.Value; // 未找到对应值，保持原样
        });
    }

    /// <inheritdoc/>
    public async Task<FileOperationResult> ReplaceVariablesInFileAsync(string filePath, IDictionary<string, string>? variables)
    {
        var result = new FileOperationResult
        {
            FilePath = filePath,
            IsSuccess = false
        };

        try
        {
            if (!File.Exists(filePath))
            {
                result.ErrorMessage = "文件不存在";
                return result;
            }

            var content = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            var replacedContent = ReplaceVariables(content, variables);
            
            // 只有内容发生变化时才写入文件
            if (!ReferenceEquals(content, replacedContent) && content != replacedContent)
            {
                await File.WriteAllTextAsync(filePath, replacedContent, Encoding.UTF8);
                result.Changed = true;
                _logger.LogInformation("已替换文件 {FilePath} 中的变量", filePath);
            }
            else
            {
                result.Changed = false;
                _logger.LogDebug("文件 {FilePath} 中未发现需要替换的变量", filePath);
            }

            result.IsSuccess = true;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "替换文件 {FilePath} 中的变量时发生错误", filePath);
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<DirectoryOperationResult> ReplaceVariablesInDirectoryAsync(string directoryPath, IDictionary<string, string>? variables)
    {
        var result = new DirectoryOperationResult
        {
            DirectoryPath = directoryPath,
            IsSuccess = false
        };

        try
        {
            if (!Directory.Exists(directoryPath))
            {
                result.ErrorMessage = "目录不存在";
                return result;
            }

            var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
            var fileResults = new List<FileOperationResult>();

            foreach (var file in files)
            {
                // 跳过二进制文件
                if (IsBinaryFile(file))
                {
                    _logger.LogDebug("跳过二进制文件 {FilePath}", file);
                    continue;
                }

                var fileResult = await ReplaceVariablesInFileAsync(file, variables);
                fileResults.Add(fileResult);

                if (!fileResult.IsSuccess)
                {
                    result.ErrorMessage = $"处理文件 {file} 时发生错误: {fileResult.ErrorMessage}";
                    return result;
                }
            }

            result.FileResults = fileResults;
            result.IsSuccess = true;
            _logger.LogInformation("已完成目录 {DirectoryPath} 中所有文件的变量替换", directoryPath);
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "替换目录 {DirectoryPath} 中的变量时发生错误", directoryPath);
        }

        return result;
    }

    /// <summary>
    /// 简单判断是否为二进制文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>是否为二进制文件</returns>
    private static bool IsBinaryFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        // 常见的二进制文件扩展名
        var binaryExtensions = new[] { ".exe", ".dll", ".bin", ".png", ".jpg", ".jpeg", ".gif", ".ico", ".zip", ".rar", ".7z" };
        
        foreach (var binaryExt in binaryExtensions)
        {
            if (extension == binaryExt)
            {
                return true;
            }
        }

        // 对于未知类型的文件，简单检查前几个字节
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var buffer = new byte[1024];
            var bytesRead = stream.Read(buffer, 0, buffer.Length);
            
            for (var i = 0; i < bytesRead; i++)
            {
                // 如果包含空字节，则很可能是二进制文件
                if (buffer[i] == 0)
                {
                    return true;
                }
            }
        }
        catch
        {
            // 出错时假设不是二进制文件
            return false;
        }

        return false;
    }
}