using Deck.Core.Models;

namespace Deck.Core.Interfaces;

/// <summary>
/// 控制台显示服务接口 - 交互式UI基础设施
/// 提供统一的控制台显示、用户交互和用户体验优化功能
/// </summary>
public interface IConsoleDisplay
{
    // ===== 基础显示功能 =====
    
    /// <summary>
    /// 显示彩色文本
    /// </summary>
    /// <param name="text">文本内容</param>
    /// <param name="color">文本颜色</param>
    void WriteLine(string text, ConsoleColor color = ConsoleColor.Gray);
    
    /// <summary>
    /// 显示彩色文本（不换行）
    /// </summary>
    /// <param name="text">文本内容</param>
    /// <param name="color">文本颜色</param>
    void Write(string text, ConsoleColor color = ConsoleColor.Gray);
    
    /// <summary>
    /// 清空控制台
    /// </summary>
    void Clear();
    
    /// <summary>
    /// 换行
    /// </summary>
    void WriteLine();
    
    // ===== 消息显示功能 =====
    
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
    /// 显示调试消息（仅在调试模式下显示）
    /// </summary>
    /// <param name="message">消息内容</param>
    void ShowDebug(string message);
    
    // ===== 标题和分隔符 =====
    
    /// <summary>
    /// 显示大标题
    /// </summary>
    /// <param name="title">标题内容</param>
    void ShowTitle(string title);
    
    /// <summary>
    /// 显示小标题
    /// </summary>
    /// <param name="subtitle">小标题内容</param>
    void ShowSubtitle(string subtitle);
    
    /// <summary>
    /// 显示分隔线
    /// </summary>
    /// <param name="length">分隔线长度（默认为控制台宽度）</param>
    /// <param name="character">分隔符字符</param>
    void ShowSeparator(int? length = null, char character = '=');
    
    // ===== 表格和列表显示 =====
    
    /// <summary>
    /// 显示表格
    /// </summary>
    /// <param name="headers">表头</param>
    /// <param name="rows">表格行</param>
    /// <param name="includeIndex">是否包含序号列</param>
    void ShowTable(string[] headers, string[][] rows, bool includeIndex = true);
    
    /// <summary>
    /// 显示项目列表
    /// </summary>
    /// <param name="items">列表项</param>
    /// <param name="includeIndex">是否包含序号</param>
    void ShowList<T>(IEnumerable<T> items, bool includeIndex = true) where T : notnull;
    
    /// <summary>
    /// 显示选择列表
    /// </summary>
    /// <param name="items">可选择的项目</param>
    /// <param name="title">列表标题</param>
    void ShowSelectableList<T>(IEnumerable<T> items, string? title = null) where T : ISelectableItem;
    
    // ===== 进度和状态显示 =====
    
    /// <summary>
    /// 显示进度条
    /// </summary>
    /// <param name="current">当前进度值</param>
    /// <param name="total">总进度值</param>
    /// <param name="message">进度描述</param>
    void ShowProgress(int current, int total, string? message = null);
    
    /// <summary>
    /// 显示步骤进度
    /// </summary>
    /// <param name="stepNumber">当前步骤</param>
    /// <param name="totalSteps">总步骤数</param>
    /// <param name="description">步骤描述</param>
    void ShowStep(int stepNumber, int totalSteps, string description);
    
    /// <summary>
    /// 显示加载动画（阻塞式）
    /// </summary>
    /// <param name="message">加载消息</param>
    /// <param name="task">要执行的异步任务</param>
    /// <returns>任务结果</returns>
    Task<T> ShowLoadingAsync<T>(string message, Func<Task<T>> task);
    
    /// <summary>
    /// 显示加载动画（阻塞式）
    /// </summary>
    /// <param name="message">加载消息</param>
    /// <param name="task">要执行的任务</param>
    Task ShowLoadingAsync(string message, Func<Task> task);
    
    /// <summary>
    /// 显示加载旋转器（单行）
    /// </summary>
    /// <param name="message">加载消息</param>
    /// <returns>加载器上下文，用于停止加载器</returns>
    IDisposable ShowSpinner(string message);
    
    // ===== 用户交互功能 =====
    
    /// <summary>
    /// 获取用户输入
    /// </summary>
    /// <param name="prompt">提示信息</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>用户输入的内容</returns>
    string? PromptInput(string prompt, string? defaultValue = null);
    
    /// <summary>
    /// 获取用户输入（密码模式）
    /// </summary>
    /// <param name="prompt">提示信息</param>
    /// <returns>用户输入的密码</returns>
    string? PromptPassword(string prompt);
    
    /// <summary>
    /// 获取用户确认
    /// </summary>
    /// <param name="message">确认消息</param>
    /// <param name="defaultValue">默认值（true为yes，false为no）</param>
    /// <returns>用户确认结果</returns>
    bool PromptConfirmation(string message, bool defaultValue = true);
    
    /// <summary>
    /// 单项选择
    /// </summary>
    /// <param name="items">选择项</param>
    /// <param name="prompt">提示信息</param>
    /// <returns>选择的项目，如果取消则返回null</returns>
    T? PromptSelection<T>(IEnumerable<T> items, string prompt) where T : ISelectableItem;
    
    /// <summary>
    /// 多项选择
    /// </summary>
    /// <param name="items">选择项</param>
    /// <param name="prompt">提示信息</param>
    /// <param name="minSelection">最少选择数量</param>
    /// <param name="maxSelection">最多选择数量（null表示无限制）</param>
    /// <returns>选择的项目列表</returns>
    IList<T> PromptMultiSelection<T>(IEnumerable<T> items, string prompt, int minSelection = 0, int? maxSelection = null) where T : ISelectableItem;
    
    /// <summary>
    /// 带搜索的选择
    /// </summary>
    /// <param name="items">选择项</param>
    /// <param name="prompt">提示信息</param>
    /// <param name="searchPlaceholder">搜索占位符</param>
    /// <returns>选择的项目，如果取消则返回null</returns>
    T? PromptSearchSelection<T>(IEnumerable<T> items, string prompt, string searchPlaceholder = "输入搜索关键词...") where T : ISelectableItem;
    
    // ===== 键盘输入处理 =====
    
    /// <summary>
    /// 等待用户按任意键继续
    /// </summary>
    /// <param name="message">等待消息</param>
    void WaitForAnyKey(string message = "按任意键继续...");
    
    /// <summary>
    /// 等待用户按指定键
    /// </summary>
    /// <param name="expectedKey">期望的按键</param>
    /// <param name="message">等待消息</param>
    /// <returns>用户是否按了正确的键</returns>
    bool WaitForSpecificKey(ConsoleKey expectedKey, string? message = null);
    
    // ===== 格式化和美化 =====
    
    /// <summary>
    /// 显示带边框的内容
    /// </summary>
    /// <param name="content">内容</param>
    /// <param name="title">边框标题</param>
    void ShowBox(string content, string? title = null);
    
    /// <summary>
    /// 显示带边框的多行内容
    /// </summary>
    /// <param name="lines">内容行</param>
    /// <param name="title">边框标题</param>
    void ShowBox(IEnumerable<string> lines, string? title = null);
    
    /// <summary>
    /// 显示带图标的消息
    /// </summary>
    /// <param name="icon">图标</param>
    /// <param name="message">消息</param>
    /// <param name="color">颜色</param>
    void ShowIconMessage(string icon, string message, ConsoleColor color = ConsoleColor.Gray);
}