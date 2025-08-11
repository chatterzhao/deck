using Deck.Core.Interfaces;
using Deck.Core.Models;

namespace Deck.Services;

/// <summary>
/// 控制台用户界面服务实现
/// </summary>
public class ConsoleUIService : IConsoleUIService
{
    // 颜色常量
    private const string Yellow = "\u001b[33m";
    private const string Green = "\u001b[32m";
    private const string Red = "\u001b[31m";
    private const string Cyan = "\u001b[36m";
    private const string Blue = "\u001b[34m";
    private const string Reset = "\u001b[0m";

    public StartCommandSelectableOption? ShowThreeLayerSelection(StartCommandThreeLayerOptions options)
    {
        Console.WriteLine();
        
        // 显示环境类型信息
        if (options.IsAutoDetected)
        {
            // Console.WriteLine($"\n🚀 启动开发环境（自动检测）");
            if (options.EnvType != "unknown")
            {
                ShowInfo($"🔍 检测到环境类型：{options.EnvType}");
                ShowWarning("💡 推荐选择对应的环境类型配置");
            }
        }
        else
        {
            Console.WriteLine($"\n🚀 启动 {options.EnvType} 开发环境");
        }
        Console.WriteLine();

        var allOptions = options.GetAllOptions();

        // 显示已构建镜像
        ShowSection("【已构建镜像 - Images】", options.Images.Cast<object>().ToList(), allOptions);

        // 显示用户自定义配置
        ShowSection("【用户自定义配置 - Custom】", options.Configs.Cast<object>().ToList(), allOptions);

        // 显示模板库
        ShowSection("【模板库 - Templates】", options.Templates.Cast<object>().ToList(), allOptions);

        // 获取用户选择
        return GetUserChoice(allOptions);
    }

    public TemplateWorkflowType ShowTemplateWorkflowSelection()
    {
        Console.WriteLine();
        ShowInfo("📋 请选择模板使用方式：");
        Console.WriteLine($"  {Green}1) 创建可编辑配置{Reset} - 复制模板到 custom 目录，可修改后使用（适合开发调试）");
        Console.WriteLine($"  {Green}2) 直接构建启动{Reset} - 使用模板配置立即构建并启动容器（适合快速测试）");
        Console.WriteLine();

        while (true)
        {
            Console.Write($"{Cyan}❓ 请选择工作流程 (1-2)：{Reset}");
            var input = Console.ReadLine();

            if (input == "1")
            {
                return TemplateWorkflowType.CreateEditableConfig;
            }
            else if (input == "2")
            {
                return TemplateWorkflowType.DirectBuildAndStart;
            }
            else
            {
                ShowError("❌ 请输入 1 或 2");
            }
        }
    }

    public bool ShowConfirmation(string message)
    {
        while (true)
        {
            Console.Write($"{Cyan}{message} (y/n): {Reset}");
            var input = Console.ReadLine()?.ToLower();

            if (input == "y" || input == "yes")
            {
                return true;
            }
            else if (input == "n" || input == "no")
            {
                return false;
            }
            else
            {
                ShowError("请输入 y 或 n");
            }
        }
    }

    public void ShowSuccess(string message)
    {
        Console.WriteLine($"{Green}{message}{Reset}");
    }

    public void ShowError(string message)
    {
        Console.WriteLine($"{Red}{message}{Reset}");
    }

    public void ShowWarning(string message)
    {
        Console.WriteLine($"{Yellow}{message}{Reset}");
    }

    public void ShowInfo(string message)
    {
        Console.WriteLine($"{Cyan}{message}{Reset}");
    }

    public void ShowDevelopmentInfo(string imageName, string containerName, DevelopmentInfo devInfo)
    {
        Console.WriteLine();
        ShowSuccess($"🚀 进入开发容器: {containerName}");
        Console.WriteLine();
        
        ShowInfo("📋 开发环境信息：");
        Console.WriteLine($"  🌐 开发服务：{devInfo.DevUrl}");
        Console.WriteLine($"  🐛 调试端口：{devInfo.DebugPort}");
        Console.WriteLine($"  📱 Web端口：{devInfo.WebUrl}");
        Console.WriteLine();
        
        ShowInfo("🛠️ 常用开发命令：");
        Console.WriteLine("  # 进入容器开发环境：");
        Console.WriteLine($"  podman exec -it $(podman ps -q -f name={imageName}-dev) bash");
        Console.WriteLine("  # 退出容器环境");
        Console.WriteLine("  exit");
        Console.WriteLine();
        
        Console.WriteLine("  # 在容器内常用命令：");
        Console.WriteLine("  dotnet new avalonia -n MyApp    # 创建新项目");
        Console.WriteLine("  dotnet build                    # 构建项目");
        Console.WriteLine("  dotnet run                      # 运行项目");
        Console.WriteLine("  dotnet watch                    # 监控文件变化并重启");
        Console.WriteLine();
        
        ShowInfo("📁 开发目录结构：");
        Console.WriteLine("  容器内工作目录：/workspace");
        Console.WriteLine($"  宿主机项目目录：{Directory.GetCurrentDirectory()}");
        Console.WriteLine();
        
        ShowWarning("💡 开发提示：");
        Console.WriteLine("  - 容器内的 /workspace 目录映射到当前宿主机目录");
        Console.WriteLine("  - 在宿主机编辑代码，容器内自动同步");
        Console.WriteLine("  - 可在 VS Code 中使用 Remote-Containers 扩展连接开发");
        Console.WriteLine($"  - 如需修改端口配置，编辑：{Path.Combine(".deck/images", imageName, ".env")}");
    }

    public void ShowStep(int stepNumber, int totalSteps, string description)
    {
        ShowInfo($"步骤 {stepNumber}/{totalSteps}: {description}");
    }

    private void ShowSection<T>(string title, List<T> items, List<StartCommandSelectableOption> allOptions)
    {
        Console.WriteLine($"{Yellow}{title}{Reset}");
        
        if (!items.Any())
        {
            Console.WriteLine("  无");
        }
        else
        {
            var sectionOptions = allOptions.Where(o => GetSectionType(o.Type) == GetSectionTypeFromTitle(title)).ToList();
            
            foreach (var option in sectionOptions)
            {
                var prefix = $"  {Cyan}{option.Number}.{Reset}";
                
                if (option.IsAvailable)
                {
                    Console.WriteLine($"{prefix} {Green}{option.DisplayName}{Reset}");
                }
                else
                {
                    Console.WriteLine($"{prefix} {Red}{option.DisplayName} (不可用，{option.UnavailableReason}){Reset}");
                }
            }
        }
        
        Console.WriteLine();
    }

    private static string GetSectionType(OptionType type)
    {
        return type switch
        {
            OptionType.Image => "Images",
            OptionType.Config => "Custom",
            OptionType.Template => "Templates",
            _ => "Unknown"
        };
    }

    private static string GetSectionTypeFromTitle(string title)
    {
        if (title.Contains("Images")) return "Images";
        if (title.Contains("Custom")) return "Custom";
        if (title.Contains("Templates")) return "Templates";
        return "Unknown";
    }

    private StartCommandSelectableOption? GetUserChoice(List<StartCommandSelectableOption> options)
    {
        if (!options.Any())
        {
            ShowError("没有可用的选项");
            return null;
        }

        var maxNumber = options.Max(o => o.Number);

        while (true)
        {
            Console.Write($"{Cyan}❓ 请选择环境序号 (1-{maxNumber})：{Reset}");
            var input = Console.ReadLine();

            if (int.TryParse(input, out var choice) && choice >= 1 && choice <= maxNumber)
            {
                var selectedOption = options.FirstOrDefault(o => o.Number == choice);
                if (selectedOption != null)
                {
                    if (selectedOption.IsAvailable)
                    {
                        return selectedOption;
                    }
                    else
                    {
                        ShowError("❌ 您选择的选项不可用，请选择其他可用选项（绿色显示的选项）");
                        continue;
                    }
                }
            }

            ShowError($"❌ 请输入有效的序号 (1-{maxNumber})");
        }
    }

    public EnvironmentType? ShowEnvironmentSelection()
    {
        Console.WriteLine();
        ShowInfo("🌐 选择部署环境：");
        Console.WriteLine();

        var environments = new[]
        {
            new { Number = 1, Type = EnvironmentType.Development, Name = "开发环境 (Development)", Description = "开发调试，热重载，详细日志" },
            new { Number = 2, Type = EnvironmentType.Test, Name = "测试环境 (Test)", Description = "功能测试，模拟生产" },
            new { Number = 3, Type = EnvironmentType.Production, Name = "生产环境 (Production)", Description = "生产部署，性能优化" }
        };

        foreach (var env in environments)
        {
            var color = env.Type switch
            {
                EnvironmentType.Development => Green,
                EnvironmentType.Test => Yellow,
                EnvironmentType.Production => Red,
                _ => Reset
            };
            Console.WriteLine($"  {Cyan}{env.Number}.{Reset} {color}{env.Name}{Reset}");
            Console.WriteLine($"     {env.Description}");
        }

        Console.WriteLine();
        Console.Write($"{Blue}请选择环境序号 (1-3，或按 Enter 取消): {Reset}");

        while (true)
        {
            var input = Console.ReadLine()?.Trim();
            
            if (string.IsNullOrEmpty(input))
            {
                return null; // 用户取消
            }

            if (int.TryParse(input, out var number) && number >= 1 && number <= 3)
            {
                return environments[number - 1].Type;
            }

            Console.Write($"{Red}❌ 请输入有效的序号 (1-3): {Reset}");
        }
    }
}