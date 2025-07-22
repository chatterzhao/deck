using Deck.Core.Models;

namespace Deck.Core.Interfaces;

/// <summary>
/// 三层配置工作流程服务接口
/// 实现Templates、Custom、Images三层配置的工作流程管理
/// </summary>
public interface IThreeLayerWorkflowService
{
    /// <summary>
    /// 执行Templates配置工作流程
    /// 支持双工作流程：创建可编辑配置 或 直接构建启动
    /// </summary>
    /// <param name="templateName">模板名称</param>
    /// <param name="envType">环境类型</param>
    /// <returns>工作流程执行结果</returns>
    Task<WorkflowExecutionResult> ExecuteTemplateWorkflowAsync(string templateName, string envType);

    /// <summary>
    /// 执行Custom配置工作流程
    /// Custom → Images：构建新镜像流程
    /// </summary>
    /// <param name="configName">自定义配置名称</param>
    /// <returns>Custom工作流程结果</returns>
    Task<CustomWorkflowResult> ExecuteCustomConfigWorkflowAsync(string configName);

    /// <summary>
    /// 执行Images配置工作流程
    /// 智能容器管理：根据容器状态选择适当的操作
    /// </summary>
    /// <param name="imageName">镜像名称</param>
    /// <returns>Images工作流程结果</returns>
    Task<ImagesWorkflowResult> ExecuteImagesWorkflowAsync(string imageName);

    /// <summary>
    /// 验证配置状态
    /// 检查配置文件完整性和有效性
    /// </summary>
    /// <param name="configPath">配置路径</param>
    /// <returns>配置状态验证结果</returns>
    Task<ConfigurationStateResult> ValidateConfigurationStateAsync(string configPath);

    /// <summary>
    /// 更新镜像元数据
    /// 跟踪镜像构建和运行状态
    /// </summary>
    /// <param name="imageDir">镜像目录</param>
    /// <param name="metadata">元数据</param>
    Task UpdateImageMetadataAsync(string imageDir, ImageMetadata metadata);

    /// <summary>
    /// 读取镜像元数据
    /// </summary>
    /// <param name="imageDir">镜像目录</param>
    /// <returns>镜像元数据</returns>
    Task<ImageMetadata?> ReadImageMetadataAsync(string imageDir);

    /// <summary>
    /// 生成配置转换链路
    /// 显示从Templates到Custom再到Images的完整链路
    /// </summary>
    /// <param name="templateName">模板名称</param>
    /// <param name="customName">Custom配置名称</param>
    /// <param name="imageName">镜像名称</param>
    /// <returns>配置转换链路信息</returns>
    Task<List<string>> GenerateConfigurationChainAsync(string? templateName = null, string? customName = null, string? imageName = null);

    /// <summary>
    /// 显示工作流程进度
    /// 透明化流程展示：步骤 1/3、步骤 2/3、步骤 3/3
    /// </summary>
    /// <param name="currentStep">当前步骤</param>
    /// <param name="totalSteps">总步骤数</param>
    /// <param name="stepDescription">步骤描述</param>
    Task ShowWorkflowProgressAsync(int currentStep, int totalSteps, string stepDescription);
}