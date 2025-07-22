using Deck.Core.Models;

namespace Deck.Core.Interfaces;

/// <summary>
/// 控制台用户界面服务接口
/// </summary>
public interface IConsoleUIService
{
    /// <summary>
    /// 显示三层配置选择界面
    /// </summary>
    /// <param name="options">三层配置选项</param>
    /// <returns>用户选择的选项</returns>
    StartCommandSelectableOption? ShowThreeLayerSelection(StartCommandThreeLayerOptions options);

    /// <summary>
    /// 显示模板工作流程选择
    /// </summary>
    /// <returns>选择的工作流程类型</returns>
    TemplateWorkflowType ShowTemplateWorkflowSelection();

    /// <summary>
    /// 显示确认对话框
    /// </summary>
    /// <param name="message">确认消息</param>
    /// <returns>用户确认结果</returns>
    bool ShowConfirmation(string message);

    /// <summary>
    /// 显示成功消息
    /// </summary>
    /// <param name="message">消息内容</param>
    void ShowSuccess(string message);

    /// <summary>
    /// 显示错误消息
    /// </summary>
    /// <param name="message">消息内容</param>
    void ShowError(string message);

    /// <summary>
    /// 显示警告消息
    /// </summary>
    /// <param name="message">消息内容</param>
    void ShowWarning(string message);

    /// <summary>
    /// 显示信息消息
    /// </summary>
    /// <param name="message">消息内容</param>
    void ShowInfo(string message);

    /// <summary>
    /// 显示开发环境信息
    /// </summary>
    /// <param name="imageName">镜像名称</param>
    /// <param name="containerName">容器名称</param>
    /// <param name="devInfo">开发信息</param>
    void ShowDevelopmentInfo(string imageName, string containerName, DevelopmentInfo devInfo);

    /// <summary>
    /// 显示步骤信息
    /// </summary>
    /// <param name="stepNumber">步骤编号</param>
    /// <param name="totalSteps">总步骤数</param>
    /// <param name="description">步骤描述</param>
    void ShowStep(int stepNumber, int totalSteps, string description);
}