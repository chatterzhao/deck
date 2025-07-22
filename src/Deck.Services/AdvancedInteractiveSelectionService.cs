using Deck.Core.Interfaces;
using Deck.Core.Models;
using System.Text;

namespace Deck.Services;

/// <summary>
/// 高级交互式选择服务实现
/// </summary>
public class AdvancedInteractiveSelectionService : IAdvancedInteractiveSelectionService
{
    private readonly IConsoleDisplay _consoleDisplay;
    
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
    private const string Rocket = "🚀";
    private const string Search = "🔍";

    public AdvancedInteractiveSelectionService(IConsoleDisplay consoleDisplay)
    {
        _consoleDisplay = consoleDisplay;
    }

    /// <summary>
    /// 显示三层配置选择界面
    /// </summary>
    public async Task<ThreeLayerSelectionResult> ShowThreeLayerSelectionAsync(
        ThreeLayerSelector selector,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 显示项目检测信息
            if (selector.ShowProjectDetection && selector.DetectedProjectType != ProjectType.Unknown)
            {
                await ShowProjectDetectionHeaderAsync(selector.DetectedProjectType);
            }

            // 构建所有可用选项
            var allOptions = BuildThreeLayerOptions(selector);
            
            if (!allOptions.Any())
            {
                return new ThreeLayerSelectionResult 
                { 
                    IsCancelled = true,
                    ErrorMessage = "没有可用的配置选项"
                };
            }

            // 显示三层选择界面
            var selectedIndex = await ShowThreeLayerMenuAsync(allOptions, selector, cancellationToken);
            
            if (selectedIndex == -1)
            {
                return new ThreeLayerSelectionResult { IsCancelled = true };
            }

            var selectedOption = allOptions[selectedIndex];
            var result = new ThreeLayerSelectionResult
            {
                IsSuccess = true,
                SelectedConfiguration = selectedOption,
                SelectedLayerType = selectedOption.LayerType
            };

            // 如果是Templates类型，需要选择工作流程
            if (selectedOption.LayerType == ThreeLayerConfigurationType.Templates)
            {
                result.WorkflowChoice = await ShowWorkflowSelectionAsync(selectedOption.Name, cancellationToken);
                if (string.IsNullOrEmpty(result.WorkflowChoice))
                {
                    return new ThreeLayerSelectionResult { IsCancelled = true };
                }
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            return new ThreeLayerSelectionResult { IsCancelled = true };
        }
        catch (Exception ex)
        {
            return new ThreeLayerSelectionResult 
            { 
                IsCancelled = true,
                ErrorMessage = $"选择过程中发生错误: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 显示带键盘导航的高级选择菜单
    /// </summary>
    public async Task<SelectionResult<T>> ShowAdvancedSelectionAsync<T>(
        InteractiveSelector<T> selector,
        KeyboardNavigationOptions? keyboardOptions = null,
        SelectionStyle? style = null,
        CancellationToken cancellationToken = default) where T : ISelectableItem
    {
        keyboardOptions ??= new KeyboardNavigationOptions();
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

        int currentIndex = Math.Max(0, selector.DefaultIndex);
        currentIndex = Math.Min(currentIndex, selector.Items.Count - 1);

        while (!cancellationToken.IsCancellationRequested)
        {
            Console.Clear();
            DisplayAdvancedHeader(selector.Prompt, style, keyboardOptions);
            DisplayAdvancedItems(selector.Items, currentIndex, style, selector.ShowIndex, selector.ShowDescription);
            DisplayKeyboardShortcuts(keyboardOptions);

            var keyInfo = Console.ReadKey(true);
            
            switch (keyInfo.Key)
            {
                case ConsoleKey.UpArrow:
                    currentIndex = (currentIndex - 1 + selector.Items.Count) % selector.Items.Count;
                    break;
                
                case ConsoleKey.DownArrow:
                    currentIndex = (currentIndex + 1) % selector.Items.Count;
                    break;
                
                case ConsoleKey.Enter:
                    var selectedItem = selector.Items[currentIndex];
                    if (!selectedItem.IsAvailable)
                    {
                        Console.WriteLine($"{Red}❌ 该选项不可用{Reset}");
                        await Task.Delay(1000, cancellationToken);
                        continue;
                    }
                    
                    return new SelectionResult<T>
                    {
                        SelectedItem = selectedItem,
                        SelectedIndex = currentIndex,
                        SelectedItems = new List<T> { selectedItem },
                        SelectedIndices = new List<int> { currentIndex }
                    };
                
                case ConsoleKey.Escape:
                    if (!selector.Required)
                    {
                        return new SelectionResult<T> { IsCancelled = true };
                    }
                    break;
                
                case ConsoleKey.F1:
                    await ShowAdvancedHelpAsync();
                    break;
                
                default:
                    // 数字选择
                    if (char.IsDigit(keyInfo.KeyChar))
                    {
                        var numChoice = int.Parse(keyInfo.KeyChar.ToString());
                        if (numChoice >= 1 && numChoice <= selector.Items.Count)
                        {
                            currentIndex = numChoice - 1;
                        }
                    }
                    break;
            }
        }

        return new SelectionResult<T> { IsCancelled = true };
    }

    /// <summary>
    /// 显示分组选择界面
    /// </summary>
    public async Task<SelectionResult<T>> ShowGroupedSelectionAsync<T>(
        Dictionary<string, InteractiveSelector<T>> groups,
        CancellationToken cancellationToken = default) where T : ISelectableItem
    {
        if (!groups.Any())
        {
            return new SelectionResult<T> { IsCancelled = true };
        }

        // 首先选择组
        var groupNames = groups.Keys.ToList();
        var groupSelector = new InteractiveSelector<SelectableOption>
        {
            Prompt = "请选择配置分组：",
            Items = groupNames.Select((name, index) => new SelectableOption
            {
                DisplayName = name,
                Value = name,
                IsAvailable = true,
                Description = $"包含 {groups[name].Items.Count} 项配置"
            }).ToList()
        };

        var groupResult = await ShowAdvancedSelectionAsync(groupSelector, cancellationToken: cancellationToken);
        if (groupResult.IsCancelled || groupResult.SelectedItem == null)
        {
            return new SelectionResult<T> { IsCancelled = true };
        }

        var selectedGroupName = groupResult.SelectedItem.Value;
        var selectedGroup = groups[selectedGroupName];

        // 然后在选定的组中选择项目
        return await ShowAdvancedSelectionAsync(selectedGroup, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 显示智能提示选择界面
    /// </summary>
    public Task<SelectionResult<T>> ShowSmartSelectionAsync<T>(
        InteractiveSelector<T> selector,
        SmartHintOptions? smartHints = null,
        CancellationToken cancellationToken = default) where T : ISelectableItem
    {
        smartHints ??= new SmartHintOptions();

        // 显示智能提示
        if (smartHints.Enabled && smartHints.ShowActionSuggestions)
        {
            DisplaySmartHints(selector.Items);
        }

        // 使用高级选择
        return ShowAdvancedSelectionAsync(selector, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 显示工作流程选择对话框
    /// </summary>
    public Task<string?> ShowWorkflowSelectionAsync(
        string templateName,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"\n{Blue}📋 请选择模板使用方式：{Reset}");
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

            var result = input switch
            {
                "1" => "create-config",
                "2" => "direct-build",
                _ => (string?)null
            };
            return Task.FromResult(result);
        }

        return Task.FromResult((string?)null);
    }

    /// <summary>
    /// 显示帮助信息
    /// </summary>
    public Task ShowHelpAsync(
        string title,
        Dictionary<string, string> helpContent,
        CancellationToken cancellationToken = default)
    {
        Console.Clear();
        Console.WriteLine($"\n{Bold}{Cyan}📚 {title}{Reset}");
        Console.WriteLine($"{Cyan}{'='.Repeat(title.Length + 4)}{Reset}");
        Console.WriteLine();

        foreach (var (key, content) in helpContent)
        {
            Console.WriteLine($"{Yellow}{key}:{Reset}");
            Console.WriteLine($"  {content}");
            Console.WriteLine();
        }

        Console.WriteLine($"{Gray}按任意键返回...{Reset}");
        Console.ReadKey(true);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 显示项目环境检测结果
    /// </summary>
    public Task ShowProjectDetectionAsync(
        ProjectType projectType,
        string[] projectFiles,
        string[] recommendations,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"\n{Rocket} 启动开发环境（自动检测）");
        Console.WriteLine($"{Cyan}{Search} 检测到环境类型：{projectType}{Reset}");
        
        if (projectFiles.Any())
        {
            Console.WriteLine($"{Gray}检测文件：{string.Join(", ", projectFiles)}{Reset}");
        }
        
        if (recommendations.Any())
        {
            Console.WriteLine($"{Yellow}💡 推荐选择对应的环境类型配置{Reset}");
            foreach (var recommendation in recommendations)
            {
                Console.WriteLine($"   • {recommendation}");
            }
        }
        
        Console.WriteLine();
        return Task.CompletedTask;
    }

    #region Private Helper Methods

    private Task ShowProjectDetectionHeaderAsync(ProjectType projectType)
    {
        var projectFiles = GetProjectFiles(projectType);
        var recommendations = GetRecommendations(projectType);
        
        return ShowProjectDetectionAsync(projectType, projectFiles, recommendations);
    }

    private string[] GetProjectFiles(ProjectType projectType)
    {
        return projectType switch
        {
            ProjectType.Tauri => new[] { "Cargo.toml", "package.json" },
            ProjectType.Flutter => new[] { "pubspec.yaml" },
            ProjectType.Avalonia => new[] { "*.csproj" },
            ProjectType.DotNet => new[] { "*.csproj", "*.sln" },
            ProjectType.Python => new[] { "requirements.txt", "pyproject.toml" },
            ProjectType.Node => new[] { "package.json" },
            _ => Array.Empty<string>()
        };
    }

    private string[] GetRecommendations(ProjectType projectType)
    {
        return projectType switch
        {
            ProjectType.Tauri => new[] { "选择 tauri-* 相关配置", "支持跨平台桌面应用开发" },
            ProjectType.Flutter => new[] { "选择 flutter-* 相关配置", "支持移动端和桌面应用开发" },
            ProjectType.Avalonia => new[] { "选择 avalonia-* 相关配置", "支持 .NET 跨平台桌面应用" },
            ProjectType.DotNet => new[] { "选择适合的 .NET 环境配置" },
            _ => new[] { "选择通用开发环境配置" }
        };
    }

    private List<SelectableThreeLayerConfiguration> BuildThreeLayerOptions(ThreeLayerSelector selector)
    {
        var options = new List<SelectableThreeLayerConfiguration>();
        
        // 添加 Images 层选项
        if (selector.ImagesConfigurations.Any())
        {
            options.AddRange(FilterByProjectType(selector.ImagesConfigurations, selector));
        }
        
        // 添加 Custom 层选项
        if (selector.CustomConfigurations.Any())
        {
            options.AddRange(FilterByProjectType(selector.CustomConfigurations, selector));
        }
        
        // 添加 Templates 层选项
        if (selector.TemplatesConfigurations.Any())
        {
            options.AddRange(FilterByProjectType(selector.TemplatesConfigurations, selector));
        }

        return options;
    }

    private List<SelectableThreeLayerConfiguration> FilterByProjectType(
        List<SelectableThreeLayerConfiguration> configurations, 
        ThreeLayerSelector selector)
    {
        if (!selector.EnableProjectTypeFilter || selector.DetectedProjectType == ProjectType.Unknown)
        {
            return configurations;
        }

        // 优先显示匹配项目类型的配置，然后显示其他配置
        var matched = configurations.Where(c => c.DetectedProjectType == selector.DetectedProjectType).ToList();
        var others = configurations.Where(c => c.DetectedProjectType != selector.DetectedProjectType).ToList();
        
        matched.AddRange(others);
        return matched;
    }

    private async Task<int> ShowThreeLayerMenuAsync(
        List<SelectableThreeLayerConfiguration> options,
        ThreeLayerSelector selector,
        CancellationToken cancellationToken)
    {
        var optionNumber = 1;

        // 按层级分组显示
        foreach (var layerType in Enum.GetValues<ThreeLayerConfigurationType>())
        {
            var layerOptions = options.Where(o => o.LayerType == layerType).ToList();
            if (!layerOptions.Any()) continue;

            var layerTitle = selector.LayerTitles.GetValueOrDefault(layerType, layerType.ToString());
            Console.WriteLine($"{Yellow}{layerTitle}{Reset}");

            foreach (var option in layerOptions)
            {
                DisplayThreeLayerOption(option, optionNumber++);
            }
            Console.WriteLine();
        }

        // 获取用户选择
        return await GetThreeLayerChoiceAsync(options.Count, cancellationToken);
    }

    private void DisplayThreeLayerOption(SelectableThreeLayerConfiguration option, int number)
    {
        var statusIcon = option.IsAvailable ? "✅" : "❌";
        var color = option.IsAvailable ? Green : Red;
        
        Console.Write($"  {Cyan}{number}.{Reset} {color}{statusIcon} {option.DisplayName}{Reset}");
        
        if (!string.IsNullOrEmpty(option.ExtraInfo))
        {
            Console.Write($" {Gray}({option.ExtraInfo}){Reset}");
        }
        
        Console.WriteLine();
        
        if (!string.IsNullOrEmpty(option.Description))
        {
            Console.WriteLine($"     {Gray}{option.Description}{Reset}");
        }
    }

    private Task<int> GetThreeLayerChoiceAsync(int maxOptions, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            Console.Write($"{Cyan}❓ 请选择环境序号 (1-{maxOptions})：{Reset}");
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input))
            {
                continue;
            }

            if (int.TryParse(input, out var choice) && choice >= 1 && choice <= maxOptions)
            {
                return Task.FromResult(choice - 1); // 转换为0基索引
            }

            Console.WriteLine($"{Red}❌ 请输入有效的序号 (1-{maxOptions}){Reset}");
        }

        return Task.FromResult(-1);
    }

    private void DisplayAdvancedHeader(string prompt, SelectionStyle style, KeyboardNavigationOptions keyboardOptions)
    {
        Console.WriteLine($"\n{Bold}{Cyan}📋 {prompt}{Reset}");
        if (style.ShowBorder)
        {
            Console.WriteLine($"{Cyan}{'─'.Repeat(prompt.Length + 4)}{Reset}");
        }
        Console.WriteLine();
    }

    private void DisplayAdvancedItems<T>(IList<T> items, int currentIndex, SelectionStyle style, bool showIndex, bool showDescription) where T : ISelectableItem
    {
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var indexStr = showIndex ? $"{i + 1}. " : "";
            var indent = new string(' ', style.IndentSpaces);
            
            var isSelected = i == currentIndex;
            var prefix = isSelected ? "▶" : " ";
            var color = isSelected ? Cyan : (item.IsAvailable ? Green : Gray);
            var statusIcon = item.IsAvailable ? "✅" : "❌";
            
            Console.Write($"{indent}{prefix} {color}{statusIcon} {indexStr}{item.DisplayName}{Reset}");
            
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

    private void DisplayKeyboardShortcuts(KeyboardNavigationOptions keyboardOptions)
    {
        if (!keyboardOptions.Enabled) return;
        
        Console.WriteLine($"{Gray}快捷键：↑↓ 导航 | Enter 确认 | Esc 取消 | F1 帮助{Reset}");
        Console.WriteLine();
    }

    private void DisplaySmartHints<T>(IList<T> items) where T : ISelectableItem
    {
        var availableCount = items.Count(x => x.IsAvailable);
        var totalCount = items.Count;
        
        if (availableCount == 0)
        {
            Console.WriteLine($"{Red}⚠️ 没有可用的配置选项{Reset}");
        }
        else if (availableCount < totalCount)
        {
            Console.WriteLine($"{Yellow}💡 {availableCount}/{totalCount} 个配置可用，请检查不可用配置的状态{Reset}");
        }
        else
        {
            Console.WriteLine($"{Green}✅ 所有 {totalCount} 个配置都可用{Reset}");
        }
        
        Console.WriteLine();
    }

    private Task ShowAdvancedHelpAsync()
    {
        var helpContent = new Dictionary<string, string>
        {
            ["键盘导航"] = "使用上下箭头键移动选择，Enter确认，Esc取消",
            ["数字选择"] = "直接按数字键快速跳转到对应选项",
            ["搜索功能"] = "在搜索模式下输入关键词快速过滤选项",
            ["状态标识"] = "✅ 表示可用，❌ 表示不可用或缺少必要文件",
            ["三层配置"] = "Images=已构建镜像，Custom=自定义配置，Templates=模板库"
        };

        return ShowHelpAsync("交互式选择帮助", helpContent);
    }

    #endregion
}

/// <summary>
/// 字符串扩展方法
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// 重复字符
    /// </summary>
    public static string Repeat(this char character, int count)
    {
        return new string(character, count);
    }
}