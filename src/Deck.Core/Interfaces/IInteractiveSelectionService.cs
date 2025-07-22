using Deck.Core.Models;

namespace Deck.Core.Interfaces;

/// <summary>
/// 交互式选择服务接口
/// </summary>
public interface IInteractiveSelectionService
{
    /// <summary>
    /// 显示单选菜单
    /// </summary>
    /// <typeparam name="T">可选择项类型</typeparam>
    /// <param name="selector">选择器配置</param>
    /// <param name="style">显示样式</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>选择结果</returns>
    Task<SelectionResult<T>> ShowSingleSelectionAsync<T>(
        InteractiveSelector<T> selector, 
        SelectionStyle? style = null,
        CancellationToken cancellationToken = default) where T : ISelectableItem;

    /// <summary>
    /// 显示多选菜单
    /// </summary>
    /// <typeparam name="T">可选择项类型</typeparam>
    /// <param name="selector">选择器配置</param>
    /// <param name="style">显示样式</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>选择结果</returns>
    Task<SelectionResult<T>> ShowMultipleSelectionAsync<T>(
        InteractiveSelector<T> selector, 
        SelectionStyle? style = null,
        CancellationToken cancellationToken = default) where T : ISelectableItem;

    /// <summary>
    /// 显示带搜索的选择菜单
    /// </summary>
    /// <typeparam name="T">可选择项类型</typeparam>
    /// <param name="selector">选择器配置</param>
    /// <param name="searchFunc">搜索函数</param>
    /// <param name="style">显示样式</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>选择结果</returns>
    Task<SelectionResult<T>> ShowSearchableSelectionAsync<T>(
        InteractiveSelector<T> selector,
        Func<T, string, bool> searchFunc,
        SelectionStyle? style = null,
        CancellationToken cancellationToken = default) where T : ISelectableItem;

    /// <summary>
    /// 显示确认对话框
    /// </summary>
    /// <param name="message">确认消息</param>
    /// <param name="defaultValue">默认值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>确认结果</returns>
    Task<bool> ShowConfirmationAsync(
        string message, 
        bool defaultValue = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 显示输入框
    /// </summary>
    /// <param name="prompt">提示信息</param>
    /// <param name="defaultValue">默认值</param>
    /// <param name="validator">验证函数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>输入结果</returns>
    Task<string?> ShowInputAsync(
        string prompt, 
        string? defaultValue = null,
        Func<string, bool>? validator = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 显示进度条
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="total">总数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>进度报告器</returns>
    IProgress<ProgressInfo> ShowProgressBar(
        string title, 
        long total = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 显示工作流程选择
    /// Templates双工作流程：创建可编辑配置 或 直接构建启动
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>工作流程类型</returns>
    Task<WorkflowType> ShowWorkflowSelectionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 进度信息
/// </summary>
public class ProgressInfo
{
    /// <summary>
    /// 当前进度
    /// </summary>
    public long Current { get; set; }
    
    /// <summary>
    /// 总数
    /// </summary>
    public long Total { get; set; }
    
    /// <summary>
    /// 消息
    /// </summary>
    public string? Message { get; set; }
    
    /// <summary>
    /// 是否完成
    /// </summary>
    public bool IsCompleted { get; set; }
    
    /// <summary>
    /// 百分比
    /// </summary>
    public double Percentage => Total > 0 ? (double)Current / Total * 100 : 0;
}