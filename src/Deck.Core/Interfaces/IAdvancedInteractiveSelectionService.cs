using Deck.Core.Models;

namespace Deck.Core.Interfaces;

/// <summary>
/// 高级交互式选择服务接口 - 支持更复杂的选择场景
/// </summary>
public interface IAdvancedInteractiveSelectionService
{
    /// <summary>
    /// 显示三层配置选择界面
    /// </summary>
    /// <param name="selector">三层选择器配置</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>三层选择结果</returns>
    Task<ThreeLayerSelectionResult> ShowThreeLayerSelectionAsync(
        ThreeLayerSelector selector,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 显示带键盘导航的高级选择菜单
    /// </summary>
    /// <typeparam name="T">可选择项类型</typeparam>
    /// <param name="selector">选择器配置</param>
    /// <param name="keyboardOptions">键盘导航选项</param>
    /// <param name="style">显示样式</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>选择结果</returns>
    Task<SelectionResult<T>> ShowAdvancedSelectionAsync<T>(
        InteractiveSelector<T> selector,
        KeyboardNavigationOptions? keyboardOptions = null,
        SelectionStyle? style = null,
        CancellationToken cancellationToken = default) where T : ISelectableItem;

    /// <summary>
    /// 显示分组选择界面
    /// </summary>
    /// <typeparam name="T">可选择项类型</typeparam>
    /// <param name="groups">分组选择器</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>选择结果</returns>
    Task<SelectionResult<T>> ShowGroupedSelectionAsync<T>(
        Dictionary<string, InteractiveSelector<T>> groups,
        CancellationToken cancellationToken = default) where T : ISelectableItem;

    /// <summary>
    /// 显示智能提示选择界面
    /// </summary>
    /// <typeparam name="T">可选择项类型</typeparam>
    /// <param name="selector">选择器配置</param>
    /// <param name="smartHints">智能提示选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>选择结果</returns>
    Task<SelectionResult<T>> ShowSmartSelectionAsync<T>(
        InteractiveSelector<T> selector,
        SmartHintOptions? smartHints = null,
        CancellationToken cancellationToken = default) where T : ISelectableItem;

    /// <summary>
    /// 显示工作流程选择对话框 (用于Templates的双工作流程)
    /// </summary>
    /// <param name="templateName">模板名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>工作流程选择结果</returns>
    Task<string?> ShowWorkflowSelectionAsync(
        string templateName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 显示帮助信息
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="helpContent">帮助内容</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ShowHelpAsync(
        string title,
        Dictionary<string, string> helpContent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 显示项目环境检测结果
    /// </summary>
    /// <param name="projectType">检测到的项目类型</param>
    /// <param name="projectFiles">相关项目文件</param>
    /// <param name="recommendations">推荐选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ShowProjectDetectionAsync(
        ProjectType projectType,
        string[] projectFiles,
        string[] recommendations,
        CancellationToken cancellationToken = default);
}