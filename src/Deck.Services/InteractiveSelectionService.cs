using Deck.Core.Interfaces;
using Deck.Core.Models;
using System.Text;

namespace Deck.Services;

/// <summary>
/// 交互式选择服务实现
/// </summary>
public class InteractiveSelectionService : IInteractiveSelectionService
{
    // 颜色常量
    private const string Yellow = "\u001b[33m";
    private const string Green = "\u001b[32m";
    private const string Red = "\u001b[31m";
    private const string Cyan = "\u001b[36m";
    private const string Blue = "\u001b[34m";
    private const string White = "\u001b[37m";
    private const string Gray = "\u001b[90m";
    private const string Reset = "\u001b[0m";
    private const string Bold = "\u001b[1m";

    /// <summary>
    /// 显示单选菜单
    /// </summary>
    public async Task<SelectionResult<T>> ShowSingleSelectionAsync<T>(
        InteractiveSelector<T> selector, 
        SelectionStyle? style = null,
        CancellationToken cancellationToken = default) where T : ISelectableItem
    {
        style ??= new SelectionStyle();
        
        if (!selector.Items.Any())
        {
            return new SelectionResult<T> { IsCancelled = true };
        }

        var availableItems = selector.Items.Where(item => item.IsAvailable).ToList();
        if (!availableItems.Any() && selector.Required)
        {
            Console.WriteLine($"{Red}❌ 没有可用的选项{Reset}");
            return new SelectionResult<T> { IsCancelled = true };
        }

        DisplayHeader(selector.Prompt, style);
        DisplayItems(selector.Items, style, selector.ShowIndex, selector.ShowDescription);

        var selectedIndex = await GetSingleChoiceAsync(
            selector.Items.Count, 
            selector.DefaultIndex,
            selector.Required,
            cancellationToken);

        if (selectedIndex == -1)
        {
            return new SelectionResult<T> { IsCancelled = true };
        }

        var selectedItem = selector.Items[selectedIndex];
        return new SelectionResult<T>
        {
            SelectedItem = selectedItem,
            SelectedIndex = selectedIndex,
            SelectedItems = new List<T> { selectedItem },
            SelectedIndices = new List<int> { selectedIndex }
        };
    }

    /// <summary>
    /// 显示多选菜单
    /// </summary>
    public async Task<SelectionResult<T>> ShowMultipleSelectionAsync<T>(
        InteractiveSelector<T> selector, 
        SelectionStyle? style = null,
        CancellationToken cancellationToken = default) where T : ISelectableItem
    {
        style ??= new SelectionStyle();
        
        if (!selector.Items.Any())
        {
            return new SelectionResult<T> { IsCancelled = true };
        }

        DisplayHeader(selector.Prompt, style);
        DisplayItems(selector.Items, style, selector.ShowIndex, selector.ShowDescription);
        
        Console.WriteLine($"{Gray}提示：输入多个序号（如：1,3,5 或 1-3,5），输入 'q' 退出{Reset}");

        var selectedIndices = await GetMultipleChoiceAsync(
            selector.Items.Count,
            selector.Required,
            cancellationToken);

        if (!selectedIndices.Any())
        {
            return new SelectionResult<T> { IsCancelled = true };
        }

        var selectedItems = selectedIndices.Select(i => selector.Items[i]).ToList();
        return new SelectionResult<T>
        {
            SelectedItems = selectedItems,
            SelectedIndices = selectedIndices,
            SelectedItem = selectedItems.FirstOrDefault(),
            SelectedIndex = selectedIndices.FirstOrDefault()
        };
    }

    /// <summary>
    /// 显示带搜索的选择菜单
    /// </summary>
    public async Task<SelectionResult<T>> ShowSearchableSelectionAsync<T>(
        InteractiveSelector<T> selector,
        Func<T, string, bool> searchFunc,
        SelectionStyle? style = null,
        CancellationToken cancellationToken = default) where T : ISelectableItem
    {
        style ??= new SelectionStyle();
        
        var filteredItems = selector.Items.ToList();
        string currentFilter = "";

        while (!cancellationToken.IsCancellationRequested)
        {
            Console.Clear();
            DisplayHeader(selector.Prompt, style);
            
            if (!string.IsNullOrWhiteSpace(currentFilter))
            {
                Console.WriteLine($"{Cyan}🔍 搜索: {currentFilter}{Reset}");
                Console.WriteLine();
            }
            
            DisplayItems(filteredItems, style, selector.ShowIndex, selector.ShowDescription);
            
            Console.WriteLine();
            Console.WriteLine($"{Gray}输入搜索关键词或选择序号 (输入 'clear' 清除搜索, 'q' 退出):{Reset}");
            Console.Write($"{Cyan}> {Reset}");
            
            var input = Console.ReadLine()?.Trim();
            
            if (string.IsNullOrEmpty(input)) continue;
            
            if (input.ToLower() == "q")
            {
                return new SelectionResult<T> { IsCancelled = true };
            }
            
            if (input.ToLower() == "clear")
            {
                currentFilter = "";
                filteredItems = selector.Items.ToList();
                continue;
            }
            
            // 尝试解析为数字选择
            if (int.TryParse(input, out var choice) && choice >= 1 && choice <= filteredItems.Count)
            {
                var selectedItem = filteredItems[choice - 1];
                if (!selectedItem.IsAvailable)
                {
                    Console.WriteLine($"{Red}❌ 该选项不可用{Reset}");
                    await Task.Delay(1500, cancellationToken);
                    continue;
                }
                
                var originalIndex = selector.Items.IndexOf(selectedItem);
                return new SelectionResult<T>
                {
                    SelectedItem = selectedItem,
                    SelectedIndex = originalIndex,
                    SelectedItems = new List<T> { selectedItem },
                    SelectedIndices = new List<int> { originalIndex }
                };
            }
            
            // 执行搜索过滤
            currentFilter = input;
            filteredItems = selector.Items.Where(item => searchFunc(item, currentFilter)).ToList();
            
            if (!filteredItems.Any())
            {
                Console.WriteLine($"{Yellow}⚠️ 没有找到匹配的选项{Reset}");
                await Task.Delay(1500, cancellationToken);
                filteredItems = selector.Items.ToList();
                currentFilter = "";
            }
        }

        return new SelectionResult<T> { IsCancelled = true };
    }

    /// <summary>
    /// 显示确认对话框
    /// </summary>
    public Task<bool> ShowConfirmationAsync(
        string message, 
        bool defaultValue = false,
        CancellationToken cancellationToken = default)
    {
        var defaultIndicator = defaultValue ? "[Y/n]" : "[y/N]";
        
        while (!cancellationToken.IsCancellationRequested)
        {
            Console.Write($"{Cyan}❓ {message} {defaultIndicator}: {Reset}");
            var input = Console.ReadLine()?.Trim().ToLower();

            if (string.IsNullOrEmpty(input))
            {
                return Task.FromResult(defaultValue);
            }

            if (input == "y" || input == "yes")
            {
                return Task.FromResult(true);
            }
            else if (input == "n" || input == "no")
            {
                return Task.FromResult(false);
            }
            else
            {
                Console.WriteLine($"{Red}请输入 y 或 n{Reset}");
            }
        }

        return Task.FromResult(defaultValue);
    }

    /// <summary>
    /// 显示输入框
    /// </summary>
    public Task<string?> ShowInputAsync(
        string prompt, 
        string? defaultValue = null,
        Func<string, bool>? validator = null,
        CancellationToken cancellationToken = default)
    {
        var defaultIndicator = !string.IsNullOrEmpty(defaultValue) ? $" [{defaultValue}]" : "";
        
        while (!cancellationToken.IsCancellationRequested)
        {
            Console.Write($"{Cyan}📝 {prompt}{defaultIndicator}: {Reset}");
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input) && !string.IsNullOrEmpty(defaultValue))
            {
                input = defaultValue;
            }

            if (string.IsNullOrEmpty(input))
            {
                Console.WriteLine($"{Red}输入不能为空{Reset}");
                continue;
            }

            if (validator != null && !validator(input))
            {
                Console.WriteLine($"{Red}输入格式不正确，请重新输入{Reset}");
                continue;
            }

            return Task.FromResult<string?>(input);
        }

        return Task.FromResult<string?>(null);
    }

    /// <summary>
    /// 显示进度条
    /// </summary>
    public IProgress<ProgressInfo> ShowProgressBar(
        string title, 
        long total = 100,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"{Cyan}{title}{Reset}");
        
        return new Progress<ProgressInfo>(progressInfo =>
        {
            if (cancellationToken.IsCancellationRequested) return;

            var percentage = progressInfo.Percentage;
            var progressBarWidth = 40;
            var filledWidth = (int)(percentage / 100 * progressBarWidth);
            
            var progressBar = new StringBuilder();
            progressBar.Append('[');
            progressBar.Append(new string('█', filledWidth));
            progressBar.Append(new string('░', progressBarWidth - filledWidth));
            progressBar.Append(']');
            
            var progressText = $"{progressBar} {percentage:F1}% ({progressInfo.Current}/{progressInfo.Total})";
            
            if (!string.IsNullOrEmpty(progressInfo.Message))
            {
                progressText += $" - {progressInfo.Message}";
            }

            // 覆盖当前行
            Console.Write($"\r{Green}{progressText}{Reset}");
            
            if (progressInfo.IsCompleted)
            {
                Console.WriteLine($" {Green}✅ 完成{Reset}");
            }
        });
    }

    /// <summary>
    /// 显示工作流程选择
    /// Templates双工作流程：创建可编辑配置 或 直接构建启动
    /// </summary>
    public Task<WorkflowType> ShowWorkflowSelectionAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine();
        Console.WriteLine($"{Bold}{Blue}📋 请选择模板使用方式：{Reset}");
        Console.WriteLine($"  {Green}1) 创建可编辑配置{Reset} - 复制模板到 custom 目录，可修改后使用（适合开发调试）");
        Console.WriteLine($"  {Green}2) 直接构建启动{Reset} - 使用模板配置立即构建并启动容器（适合快速测试）");
        Console.WriteLine();

        while (!cancellationToken.IsCancellationRequested)
        {
            Console.Write($"{Cyan}❓ 请选择工作流程 (1-2)：{Reset}");
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input))
            {
                continue;
            }

            if (input == "1")
            {
                return Task.FromResult(WorkflowType.CreateEditableConfig);
            }
            
            if (input == "2")
            {
                return Task.FromResult(WorkflowType.DirectBuildAndStart);
            }

            Console.WriteLine($"{Red}❌ 请输入有效的选项 (1 或 2){Reset}");
        }

        // 默认返回创建可编辑配置
        return Task.FromResult(WorkflowType.CreateEditableConfig);
    }

    #region Private Helper Methods

    private void DisplayHeader(string prompt, SelectionStyle style)
    {
        Console.WriteLine();
        if (style.ShowBorder && style.BorderStyle != BorderStyle.None)
        {
            var border = GetBorderChar(style.BorderStyle, true);
            Console.WriteLine($"{GetStyleColor(style.HighlightColor)}{new string(border, prompt.Length + 4)}{Reset}");
        }
        
        Console.WriteLine($"{Bold}{Cyan}📋 {prompt}{Reset}");
        
        if (style.ShowBorder && style.BorderStyle != BorderStyle.None)
        {
            var border = GetBorderChar(style.BorderStyle, false);
            Console.WriteLine($"{GetStyleColor(style.HighlightColor)}{new string(border, prompt.Length + 4)}{Reset}");
        }
        Console.WriteLine();
    }

    private void DisplayItems<T>(IList<T> items, SelectionStyle style, bool showIndex, bool showDescription) where T : ISelectableItem
    {
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var indexStr = showIndex ? $"{i + 1}. " : "";
            var indent = new string(' ', style.IndentSpaces);
            
            var color = item.IsAvailable ? 
                GetStyleColor(style.SelectedColor) : 
                GetStyleColor(style.DisabledColor);
            
            var statusIcon = item.IsAvailable ? "✅" : "❌";
            
            Console.Write($"{indent}{color}{statusIcon} {indexStr}{item.DisplayName}{Reset}");
            
            if (!string.IsNullOrEmpty(item.ExtraInfo))
            {
                Console.Write($" {Gray}({item.ExtraInfo}){Reset}");
            }
            
            Console.WriteLine();
            
            if (showDescription && !string.IsNullOrEmpty(item.Description))
            {
                Console.WriteLine($"{indent}   {Gray}{item.Description}{Reset}");
            }
        }
        Console.WriteLine();
    }

    private Task<int> GetSingleChoiceAsync(int itemCount, int defaultIndex, bool required, CancellationToken cancellationToken)
    {
        var maxNumber = itemCount;
        
        while (!cancellationToken.IsCancellationRequested)
        {
            var prompt = required ? 
                $"❓ 请选择选项序号 (1-{maxNumber})：" : 
                $"❓ 请选择选项序号 (1-{maxNumber}，q 退出)：";
                
            Console.Write($"{Cyan}{prompt}{Reset}");
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input))
            {
                if (defaultIndex >= 0 && defaultIndex < itemCount)
                {
                    return Task.FromResult(defaultIndex);
                }
                continue;
            }

            if (!required && (input.ToLower() == "q" || input.ToLower() == "quit"))
            {
                return Task.FromResult(-1);
            }

            if (int.TryParse(input, out var choice) && choice >= 1 && choice <= maxNumber)
            {
                return Task.FromResult(choice - 1);
            }

            Console.WriteLine($"{Red}❌ 请输入有效的序号 (1-{maxNumber}){Reset}");
        }

        return Task.FromResult(-1);
    }

    private Task<List<int>> GetMultipleChoiceAsync(int itemCount, bool required, CancellationToken cancellationToken)
    {
        var selectedIndices = new List<int>();
        
        while (!cancellationToken.IsCancellationRequested)
        {
            var prompt = required ? 
                $"❓ 请选择选项序号 (1-{itemCount})：" : 
                $"❓ 请选择选项序号 (1-{itemCount}，q 退出)：";
                
            Console.Write($"{Cyan}{prompt}{Reset}");
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input))
            {
                continue;
            }

            if (!required && (input.ToLower() == "q" || input.ToLower() == "quit"))
            {
                break;
            }

            var parsedIndices = ParseMultipleChoices(input, itemCount);
            if (parsedIndices.Any())
            {
                selectedIndices.AddRange(parsedIndices);
                break;
            }

            Console.WriteLine($"{Red}❌ 请输入有效的序号格式 (如: 1,3,5 或 1-3,5){Reset}");
        }

        return Task.FromResult(selectedIndices.Distinct().OrderBy(x => x).ToList());
    }

    private List<int> ParseMultipleChoices(string input, int maxCount)
    {
        var indices = new List<int>();
        var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            var trimmedPart = part.Trim();
            
            if (trimmedPart.Contains('-'))
            {
                // 处理范围选择 (如: 1-3)
                var rangeParts = trimmedPart.Split('-', 2);
                if (rangeParts.Length == 2 &&
                    int.TryParse(rangeParts[0].Trim(), out var start) &&
                    int.TryParse(rangeParts[1].Trim(), out var end) &&
                    start >= 1 && end <= maxCount && start <= end)
                {
                    for (int i = start; i <= end; i++)
                    {
                        indices.Add(i - 1); // 转换为0基索引
                    }
                }
                else
                {
                    return new List<int>(); // 返回空列表表示解析失败
                }
            }
            else
            {
                // 处理单个数字
                if (int.TryParse(trimmedPart, out var single) && single >= 1 && single <= maxCount)
                {
                    indices.Add(single - 1); // 转换为0基索引
                }
                else
                {
                    return new List<int>(); // 返回空列表表示解析失败
                }
            }
        }

        return indices;
    }

    private char GetBorderChar(BorderStyle borderStyle, bool isTop)
    {
        return borderStyle switch
        {
            BorderStyle.Simple => '-',
            BorderStyle.Double => '=',
            BorderStyle.Rounded => isTop ? '─' : '─',
            _ => ' '
        };
    }

    private string GetStyleColor(ConsoleColor color)
    {
        return color switch
        {
            ConsoleColor.Red => Red,
            ConsoleColor.Green => Green,
            ConsoleColor.Yellow => Yellow,
            ConsoleColor.Blue => Blue,
            ConsoleColor.Cyan => Cyan,
            ConsoleColor.White => White,
            ConsoleColor.Gray => Gray,
            ConsoleColor.DarkGray => Gray,
            _ => Reset
        };
    }

    #endregion
}