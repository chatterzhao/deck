using Deck.Core.Interfaces;
using Deck.Core.Models;
using System.Diagnostics;
using System.Text;

namespace Deck.Services;

/// <summary>
/// 控制台显示服务实现 - 交互式UI基础设施
/// 基于deck-shell的用户交互体验，提供统一的控制台显示和用户交互功能
/// </summary>
public class ConsoleDisplayService : IConsoleDisplay
{
    // ===== 颜色和Unicode符号常量 =====
    
    // ANSI颜色代码（参考deck-shell的颜色定义）
    private static readonly Dictionary<ConsoleColor, string> ColorCodes = new()
    {
        { ConsoleColor.Red, "\u001b[31m" },
        { ConsoleColor.Green, "\u001b[32m" },
        { ConsoleColor.Yellow, "\u001b[33m" },
        { ConsoleColor.Blue, "\u001b[34m" },
        { ConsoleColor.Magenta, "\u001b[35m" },
        { ConsoleColor.Cyan, "\u001b[36m" },
        { ConsoleColor.White, "\u001b[37m" },
        { ConsoleColor.Gray, "\u001b[90m" },
        { ConsoleColor.DarkRed, "\u001b[91m" },
        { ConsoleColor.DarkGreen, "\u001b[92m" },
        { ConsoleColor.DarkYellow, "\u001b[93m" },
        { ConsoleColor.DarkBlue, "\u001b[94m" },
        { ConsoleColor.DarkMagenta, "\u001b[95m" },
        { ConsoleColor.DarkCyan, "\u001b[96m" },
        { ConsoleColor.Black, "\u001b[30m" },
        { ConsoleColor.DarkGray, "\u001b[97m" }
    };
    
    private const string Reset = "\u001b[0m";
    
    // Unicode符号（参考deck-shell的符号定义）
    private const string CheckMark = "✓";
    private const string CrossMark = "✗";
    private const string WarningMark = "⚠";
    private const string InfoMark = "ℹ";
    private const string QuestionMark = "❓";
    private const string Rocket = "🚀";
    private const string Gear = "⚙";
    private const string Folder = "📁";
    private const string Wrench = "🔧";
    
    // 加载旋转器字符
    private static readonly char[] SpinnerChars = "⠋⠙⠹⠸⠼⠴⠦⠧⠇⠏".ToCharArray();
    
    // ===== 基础显示功能 =====
    
    public void WriteLine(string text, ConsoleColor color = ConsoleColor.Gray)
    {
        if (color == ConsoleColor.Gray)
        {
            Console.WriteLine(text);
        }
        else
        {
            Console.WriteLine($"{GetColorCode(color)}{text}{Reset}");
        }
    }
    
    public void Write(string text, ConsoleColor color = ConsoleColor.Gray)
    {
        if (color == ConsoleColor.Gray)
        {
            Console.Write(text);
        }
        else
        {
            Console.Write($"{GetColorCode(color)}{text}{Reset}");
        }
    }
    
    public void Clear()
    {
        Console.Clear();
    }
    
    public void WriteLine()
    {
        Console.WriteLine();
    }
    
    // ===== 消息显示功能 =====
    
    public void ShowSuccess(string message)
    {
        WriteLine($"{CheckMark} {message}", ConsoleColor.Green);
    }
    
    public void ShowError(string message)
    {
        WriteLine($"{CrossMark} {message}", ConsoleColor.Red);
    }
    
    public void ShowWarning(string message)
    {
        WriteLine($"{WarningMark} {message}", ConsoleColor.Yellow);
    }
    
    public void ShowInfo(string message)
    {
        WriteLine($"{InfoMark} {message}", ConsoleColor.Cyan);
    }
    
    public void ShowDebug(string message)
    {
#if DEBUG
        WriteLine($"🐛 [DEBUG] {message}", ConsoleColor.Gray);
#endif
    }
    
    // ===== 标题和分隔符 =====
    
    public void ShowTitle(string title)
    {
        WriteLine();
        var width = Math.Max(title.Length + 4, 50);
        ShowSeparator(width, '=');
        WriteLine($"  {title}".PadRight(width - 2), ConsoleColor.Yellow);
        ShowSeparator(width, '=');
        WriteLine();
    }
    
    public void ShowSubtitle(string subtitle)
    {
        WriteLine();
        WriteLine($"── {subtitle} ──", ConsoleColor.Cyan);
        WriteLine();
    }
    
    public void ShowSeparator(int? length = null, char character = '=')
    {
        var width = length ?? Math.Min(Console.WindowWidth, 80);
        WriteLine(new string(character, width), ConsoleColor.DarkGray);
    }
    
    // ===== 表格和列表显示 =====
    
    public void ShowTable(string[] headers, string[][] rows, bool includeIndex = true)
    {
        if (headers.Length == 0 || rows.Length == 0)
        {
            ShowWarning("表格数据为空");
            return;
        }
        
        var allHeaders = includeIndex ? new[] { "#" }.Concat(headers).ToArray() : headers;
        var columnWidths = CalculateColumnWidths(allHeaders, rows, includeIndex);
        
        // 显示表头
        ShowTableRow(allHeaders, columnWidths, ConsoleColor.Yellow);
        ShowSeparator(columnWidths.Sum() + columnWidths.Length - 1, '-');
        
        // 显示数据行
        for (int i = 0; i < rows.Length; i++)
        {
            var rowData = includeIndex ? 
                new[] { (i + 1).ToString() }.Concat(rows[i]).ToArray() : 
                rows[i];
            
            ShowTableRow(rowData, columnWidths);
        }
    }
    
    public void ShowList<T>(IEnumerable<T> items, bool includeIndex = true) where T : notnull
    {
        var itemList = items.ToList();
        for (int i = 0; i < itemList.Count; i++)
        {
            var prefix = includeIndex ? $"{i + 1}. " : "• ";
            WriteLine($"  {prefix}{itemList[i]}", ConsoleColor.Gray);
        }
    }
    
    public void ShowSelectableList<T>(IEnumerable<T> items, string? title = null) where T : ISelectableItem
    {
        if (!string.IsNullOrEmpty(title))
        {
            ShowSubtitle(title);
        }
        
        var itemList = items.ToList();
        for (int i = 0; i < itemList.Count; i++)
        {
            var item = itemList[i];
            var color = item.IsAvailable ? ConsoleColor.Green : ConsoleColor.Red;
            var status = item.IsAvailable ? "" : " (不可用)";
            
            WriteLine($"  {i + 1}. {item.DisplayName}{status}", color);
            
            if (!string.IsNullOrEmpty(item.Description))
            {
                WriteLine($"     {item.Description}", ConsoleColor.Gray);
            }
        }
    }
    
    // ===== 进度和状态显示 =====
    
    public void ShowProgress(int current, int total, string? message = null)
    {
        var percentage = (double)current / total * 100;
        var progressBarWidth = 40;
        var progressChars = (int)(progressBarWidth * current / total);
        
        var progressBar = new string('█', progressChars) + new string('░', progressBarWidth - progressChars);
        var text = $"[{progressBar}] {percentage:F1}% ({current}/{total})";
        
        if (!string.IsNullOrEmpty(message))
        {
            text += $" {message}";
        }
        
        Write($"\r{text}", ConsoleColor.Cyan);
        
        if (current >= total)
        {
            WriteLine();
        }
    }
    
    public void ShowStep(int stepNumber, int totalSteps, string description)
    {
        WriteLine($"[{stepNumber}/{totalSteps}] {description}...", ConsoleColor.Cyan);
    }
    
    public async Task<T> ShowLoadingAsync<T>(string message, Func<Task<T>> task)
    {
        using var spinner = ShowSpinner(message);
        return await task();
    }
    
    public async Task ShowLoadingAsync(string message, Func<Task> task)
    {
        using var spinner = ShowSpinner(message);
        await task();
    }
    
    public IDisposable ShowSpinner(string message)
    {
        return new SpinnerContext(this, message);
    }
    
    // ===== 用户交互功能 =====
    
    public string? PromptInput(string prompt, string? defaultValue = null)
    {
        var displayPrompt = string.IsNullOrEmpty(defaultValue) ? 
            $"{QuestionMark} {prompt}: " : 
            $"{QuestionMark} {prompt} [{defaultValue}]: ";
        
        Write(displayPrompt, ConsoleColor.Cyan);
        var input = Console.ReadLine();
        
        return string.IsNullOrWhiteSpace(input) ? defaultValue : input;
    }
    
    public string? PromptPassword(string prompt)
    {
        Write($"{QuestionMark} {prompt}: ", ConsoleColor.Cyan);
        
        var password = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(true);
            
            if (key.Key == ConsoleKey.Enter)
            {
                WriteLine();
                break;
            }
            else if (key.Key == ConsoleKey.Backspace)
            {
                if (password.Length > 0)
                {
                    password.Length--;
                    Write("\b \b");
                }
            }
            else if (!char.IsControl(key.KeyChar))
            {
                password.Append(key.KeyChar);
                Write("*");
            }
        }
        
        return password.ToString();
    }
    
    public bool PromptConfirmation(string message, bool defaultValue = true)
    {
        var options = defaultValue ? "[Y/n]" : "[y/N]";
        
        while (true)
        {
            Write($"{QuestionMark} {message} {options}: ", ConsoleColor.Cyan);
            var input = Console.ReadLine()?.Trim().ToLowerInvariant();
            
            if (string.IsNullOrEmpty(input))
            {
                return defaultValue;
            }
            
            return input switch
            {
                "y" or "yes" or "是" => true,
                "n" or "no" or "否" => false,
                _ => HandleInvalidConfirmationInput()
            };
        }
        
        bool HandleInvalidConfirmationInput()
        {
            ShowError("请输入 y(是) 或 n(否)");
            return PromptConfirmation(message, defaultValue);
        }
    }
    
    public T? PromptSelection<T>(IEnumerable<T> items, string prompt) where T : ISelectableItem
    {
        var itemList = items.Where(i => i.IsAvailable).ToList();
        
        if (!itemList.Any())
        {
            ShowError("没有可选择的项目");
            return default;
        }
        
        ShowSelectableList(itemList, prompt);
        
        while (true)
        {
            Write($"{QuestionMark} 请选择 (1-{itemList.Count}): ", ConsoleColor.Cyan);
            var input = Console.ReadLine();
            
            if (int.TryParse(input, out var choice) && choice >= 1 && choice <= itemList.Count)
            {
                return itemList[choice - 1];
            }
            
            ShowError($"请输入有效的序号 (1-{itemList.Count})");
        }
    }
    
    public IList<T> PromptMultiSelection<T>(IEnumerable<T> items, string prompt, int minSelection = 0, int? maxSelection = null) where T : ISelectableItem
    {
        var itemList = items.Where(i => i.IsAvailable).ToList();
        var selected = new List<T>();
        
        if (!itemList.Any())
        {
            ShowError("没有可选择的项目");
            return selected;
        }
        
        ShowSelectableList(itemList, prompt);
        ShowInfo($"请输入选择的序号，用空格分隔 (最少{minSelection}项{(maxSelection.HasValue ? $"，最多{maxSelection}项" : "")})：");
        
        while (true)
        {
            Write($"{QuestionMark} 选择: ", ConsoleColor.Cyan);
            var input = Console.ReadLine()?.Trim();
            
            if (string.IsNullOrEmpty(input))
            {
                if (minSelection == 0)
                {
                    break;
                }
                ShowError($"至少需要选择 {minSelection} 项");
                continue;
            }
            
            var choices = input.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                               .Select(s => int.TryParse(s, out var i) ? i : -1)
                               .Where(i => i >= 1 && i <= itemList.Count)
                               .Distinct()
                               .ToList();
            
            if (choices.Count < minSelection)
            {
                ShowError($"至少需要选择 {minSelection} 项");
                continue;
            }
            
            if (maxSelection.HasValue && choices.Count > maxSelection.Value)
            {
                ShowError($"最多只能选择 {maxSelection.Value} 项");
                continue;
            }
            
            selected = choices.Select(c => itemList[c - 1]).ToList();
            break;
        }
        
        return selected;
    }
    
    public T? PromptSearchSelection<T>(IEnumerable<T> items, string prompt, string searchPlaceholder = "输入搜索关键词...") where T : ISelectableItem
    {
        var allItems = items.Where(i => i.IsAvailable).ToList();
        
        while (true)
        {
            Write($"{QuestionMark} {searchPlaceholder} ", ConsoleColor.Cyan);
            var searchTerm = Console.ReadLine()?.Trim();
            
            if (string.IsNullOrEmpty(searchTerm))
            {
                return PromptSelection(allItems, prompt);
            }
            
            var filteredItems = allItems.Where(item => 
                item.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (item.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();
            
            if (!filteredItems.Any())
            {
                ShowWarning($"没有找到包含 '{searchTerm}' 的项目，请重新搜索");
                continue;
            }
            
            return PromptSelection(filteredItems, $"搜索结果 ('{searchTerm}'):");
        }
    }
    
    // ===== 键盘输入处理 =====
    
    public void WaitForAnyKey(string message = "按任意键继续...")
    {
        ShowInfo(message);
        Console.ReadKey(true);
    }
    
    public bool WaitForSpecificKey(ConsoleKey expectedKey, string? message = null)
    {
        var displayMessage = message ?? $"按 {expectedKey} 键继续...";
        ShowInfo(displayMessage);
        
        var key = Console.ReadKey(true);
        return key.Key == expectedKey;
    }
    
    // ===== 格式化和美化 =====
    
    public void ShowBox(string content, string? title = null)
    {
        ShowBox([content], title);
    }
    
    public void ShowBox(IEnumerable<string> lines, string? title = null)
    {
        var lineList = lines.ToList();
        var maxWidth = lineList.Max(l => l.Length);
        var boxWidth = Math.Max(maxWidth + 4, (title?.Length ?? 0) + 6);
        
        // 顶部边框
        if (!string.IsNullOrEmpty(title))
        {
            WriteLine($"┌─ {title} ".PadRight(boxWidth - 1, '─') + "┐", ConsoleColor.Cyan);
        }
        else
        {
            WriteLine("┌" + new string('─', boxWidth - 2) + "┐", ConsoleColor.Cyan);
        }
        
        // 内容行
        foreach (var line in lineList)
        {
            WriteLine($"│ {line}".PadRight(boxWidth - 1) + "│", ConsoleColor.Gray);
        }
        
        // 底部边框
        WriteLine("└" + new string('─', boxWidth - 2) + "┘", ConsoleColor.Cyan);
    }
    
    public void ShowIconMessage(string icon, string message, ConsoleColor color = ConsoleColor.Gray)
    {
        WriteLine($"{icon} {message}", color);
    }
    
    // ===== 辅助方法 =====
    
    private static string GetColorCode(ConsoleColor color)
    {
        return ColorCodes.TryGetValue(color, out var code) ? code : "";
    }
    
    private static int[] CalculateColumnWidths(string[] headers, string[][] rows, bool includeIndex)
    {
        var widths = new int[headers.Length];
        
        // 计算标题宽度
        for (int i = 0; i < headers.Length; i++)
        {
            widths[i] = headers[i].Length;
        }
        
        // 计算数据行宽度
        foreach (var row in rows)
        {
            var dataRow = includeIndex ? new[] { "999" }.Concat(row).ToArray() : row;
            for (int i = 0; i < Math.Min(dataRow.Length, widths.Length); i++)
            {
                widths[i] = Math.Max(widths[i], dataRow[i]?.Length ?? 0);
            }
        }
        
        return widths;
    }
    
    private void ShowTableRow(string[] columns, int[] widths, ConsoleColor color = ConsoleColor.Gray)
    {
        var row = string.Join(" │ ", columns.Select((col, i) => 
            i < widths.Length ? (col ?? "").PadRight(widths[i]) : col ?? ""));
        WriteLine($" {row} ", color);
    }
    
    // ===== 内部类 - 加载旋转器上下文 =====
    
    private class SpinnerContext : IDisposable
    {
        private readonly ConsoleDisplayService _display;
        private readonly string _message;
        private readonly CancellationTokenSource _cancellation;
        private readonly Task _spinTask;
        
        public SpinnerContext(ConsoleDisplayService display, string message)
        {
            _display = display;
            _message = message;
            _cancellation = new CancellationTokenSource();
            
            _spinTask = Task.Run(SpinAsync);
        }
        
        private async Task SpinAsync()
        {
            var cursorLeft = Console.CursorLeft;
            var cursorTop = Console.CursorTop;
            var spinnerIndex = 0;
            
            try
            {
                while (!_cancellation.Token.IsCancellationRequested)
                {
                    var spinnerChar = SpinnerChars[spinnerIndex % SpinnerChars.Length];
                    
                    Console.SetCursorPosition(cursorLeft, cursorTop);
                    _display.Write($"{spinnerChar} {_message}", ConsoleColor.Cyan);
                    
                    spinnerIndex++;
                    await Task.Delay(100, _cancellation.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
            finally
            {
                // Clear the spinner line and show completion
                Console.SetCursorPosition(cursorLeft, cursorTop);
                _display.WriteLine($"{CheckMark} {_message} 完成", ConsoleColor.Green);
            }
        }
        
        public void Dispose()
        {
            _cancellation.Cancel();
            try
            {
                _spinTask.Wait(1000); // Wait up to 1 second for cleanup
            }
            catch (AggregateException ex) when (ex.InnerException is OperationCanceledException)
            {
                // Expected exception, ignore
            }
            
            _cancellation.Dispose();
        }
    }
}