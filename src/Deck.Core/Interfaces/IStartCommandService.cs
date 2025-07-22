using Deck.Core.Models;

namespace Deck.Core.Interfaces;

/// <summary>
/// Start 命令服务接口 - 处理三层配置选择和启动逻辑
/// </summary>
public interface IStartCommandService
{
    /// <summary>
    /// 执行 start 命令
    /// </summary>
    /// <param name="envType">环境类型，null表示自动检测</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>执行结果</returns>
    Task<StartCommandResult> ExecuteAsync(string? envType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取三层配置选项列表
    /// </summary>
    /// <param name="envType">环境类型过滤器</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>配置选项列表</returns>
    Task<StartCommandThreeLayerOptions> GetOptionsAsync(string? envType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 从镜像启动
    /// </summary>
    /// <param name="imageName">镜像名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>启动结果</returns>
    Task<StartCommandResult> StartFromImageAsync(string imageName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 从配置构建并启动
    /// </summary>
    /// <param name="configName">配置名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>启动结果</returns>
    Task<StartCommandResult> StartFromConfigAsync(string configName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 从模板创建并启动
    /// </summary>
    /// <param name="templateName">模板名称</param>
    /// <param name="envType">环境类型</param>
    /// <param name="workflowType">工作流程类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>启动结果</returns>
    Task<StartCommandResult> StartFromTemplateAsync(string templateName, string? envType, TemplateWorkflowType workflowType, CancellationToken cancellationToken = default);
}