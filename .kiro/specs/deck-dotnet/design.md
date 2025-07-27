# .NET Console重构设计文档

## 概述

本设计文档描述了将Shell脚本版本的deck工具重构为.NET 9 Console应用程序的技术架构。重构的核心目标是在保持CLI体验一致性的基础上，通过**交互式选择**、**三层统一管理**、**智能清理逻辑**等优化特性，提供比原Shell版本更优秀的用户体验，同时实现跨平台支持、AOT原生性能和更好的可维护性。

**重构核心优化目标：**
- **交互式体验优化** - 所有需要参数的命令支持无参数交互式选择
- **三层统一管理** - `deck images` 系列统一管理Deck配置+Podman镜像+容器
- **智能清理系统** - 不同层级选择对应不同的清理策略和警告机制
- **标准平台包分发** - MSI/DMG/DEB/RPM标准安装包，无需手动配置PATH
- **AOT原生性能** - .NET 9 AOT编译，启动迅速，低资源占用

## 架构设计

### 整体架构

采用分层架构模式，将应用程序分为以下层次：

```
┌─────────────────────────────────────┐
│           CLI Layer                 │  ← 命令行接口和用户交互
├─────────────────────────────────────┤
│         Service Layer               │  ← 业务逻辑和核心服务
├─────────────────────────────────────┤
│        Infrastructure Layer         │  ← 外部系统集成和工具
├─────────────────────────────────────┤
│          Core Layer                 │  ← 领域模型和共享组件
└─────────────────────────────────────┘
```

### 项目结构

```
deck-dotnet/
├── src/
│   ├── Deck.Console/                 # 主控制台应用
│   │   ├── Commands/                 # 命令处理器
│   │   │   ├── StartCommand.cs       # 智能启动命令（三层配置选择）
│   │   │   ├── StopCommand.cs        # 停止命令（交互式选择）
│   │   │   ├── ImagesCommand.cs      # 镜像三层统一管理命令
│   │   │   ├── CleanCommand.cs       # 智能清理命令
│   │   │   ├── InteractiveCommands.cs # 交互式选择基类
│   │   │   └── ...
│   │   ├── UI/                       # 用户交互界面
│   │   │   ├── InteractiveSelector.cs # 交互式选择组件
│   │   │   ├── ProgressDisplay.cs     # 进度显示组件
│   │   │   └── ConsoleFormatter.cs    # 控制台格式化
│   │   ├── Program.cs               # 应用入口点
│   │   └── Deck.Console.csproj
│   ├── Deck.Core/                   # 核心领域模型
│   │   ├── Models/                  # 数据模型
│   │   │   ├── ThreeLayerModels.cs   # 三层配置模型
│   │   │   ├── InteractiveModels.cs  # 交互式选择模型
│   │   │   └── ...
│   │   ├── Interfaces/              # 接口定义
│   │   ├── Exceptions/              # 自定义异常
│   │   └── Deck.Core.csproj
│   ├── Deck.Services/               # 业务服务层
│   │   ├── ContainerService.cs      # 容器管理服务
│   │   ├── TemplateService.cs       # 模板管理服务
│   │   ├── ConfigurationService.cs  # 配置管理服务
│   │   ├── ImagesUnifiedService.cs  # 三层统一管理服务
│   │   ├── InteractiveService.cs    # 交互式选择服务
│   │   ├── CleaningService.cs       # 智能清理服务
│   │   └── Deck.Services.csproj
│   └── Deck.Infrastructure/         # 基础设施层
│       ├── FileSystem/              # 文件系统操作
│       ├── Git/                     # Git操作
│       ├── Container/               # 容器引擎集成
│       ├── System/                  # 系统检测
│       ├── Interactive/             # 交互式UI基础设施
│       └── Deck.Infrastructure.csproj
├── tests/                           # 单元测试
│   ├── Deck.Console.Tests/
│   ├── Deck.Services.Tests/
│   └── Deck.Infrastructure.Tests/
├── build/                           # 构建脚本和配置
│   ├── package-scripts/             # 平台包构建脚本
│   └── aot-configs/                 # AOT编译配置
└── Directory.Build.props            # .NET 9统一配置
```

## 核心组件设计

### 1. CLI层设计 - 交互式命令系统

使用**System.CommandLine**库实现现代化的命令行接口，支持**无参数交互式选择**优化：

```csharp
// Program.cs
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("开发环境容器化工具");
        
        // 注册命令
        rootCommand.AddCommand(new StartCommand());
        rootCommand.AddCommand(new StopCommand());
        rootCommand.AddCommand(new LogsCommand());
        rootCommand.AddCommand(new ShellCommand());
        rootCommand.AddCommand(new DoctorCommand());
        
        // 配置依赖注入
        var services = ConfigureServices();
        
        return await rootCommand.InvokeAsync(args);
    }
}

// Commands/StartCommand.cs - 三层配置智能启动
public class StartCommand : InteractiveCommandBase
{
    private readonly IImagesUnifiedService _imagesService;
    private readonly IInteractiveService _interactiveService;
    
    public StartCommand(IImagesUnifiedService imagesService, IInteractiveService interactiveService) 
        : base("start", "智能启动开发环境（三层配置选择）")
    {
        var envTypeArgument = new Argument<string?>("env-type", "环境类型筛选（可选）");
        AddArgument(envTypeArgument);
        
        this.SetHandler(HandleAsync, envTypeArgument);
    }
    
    private async Task HandleAsync(string? envType)
    {
        // 1. 显示三层配置选择界面
        var threeLayerOptions = await _imagesService.GetThreeLayerOptionsAsync(envType);
        
        // 2. 交互式选择配置
        var selection = await _interactiveService.SelectFromThreeLayersAsync(threeLayerOptions);
        
        // 3. 根据选择类型执行对应逻辑
        await selection.ConfigType switch
        {
            ConfigurationType.Images => StartFromImageAsync(selection),
            ConfigurationType.Custom => StartFromCustomAsync(selection),
            ConfigurationType.Templates => StartFromTemplateAsync(selection),
            _ => throw new InvalidOperationException($"未知配置类型: {selection.ConfigType}")
        };
    }
}

// Commands/StopCommand.cs - 停止容器（交互式选择）
public class StopCommand : InteractiveCommandBase
{
    private readonly IContainerService _containerService;
    private readonly IInteractiveService _interactiveService;
    
    public StopCommand(IContainerService containerService, IInteractiveService interactiveService)
        : base("stop", "停止指定容器（支持交互式选择）")
    {
        var containerArgument = new Argument<string?>("container", "容器名称或ID（可选）");
        AddArgument(containerArgument);
        
        this.SetHandler(HandleAsync, containerArgument);
    }
    
    private async Task HandleAsync(string? containerName)
    {
        if (string.IsNullOrEmpty(containerName))
        {
            // 无参数交互式选择
            var containers = await _containerService.ListProjectRelatedContainersAsync();
            var runningContainers = containers.Where(c => c.Status == ContainerStatus.Running);
            
            if (!runningContainers.Any())
            {
                Console.WriteLine("没有正在运行的容器");
                return;
            }
            
            var selectedContainer = await _interactiveService.SelectFromListAsync(
                "请选择要停止的容器：", runningContainers);
                
            await StopContainerAsync(selectedContainer);
        }
        else
        {
            // 直接停止指定容器
            await StopContainerAsync(new ContainerInfo { Name = containerName });
        }
    }
    
    private async Task StopContainerAsync(ContainerInfo container)
    {
        await _containerService.StopContainerAsync(container.Name);
        await _interactiveService.DisplayPodmanCommandHintAsync(
            $"podman stop {container.Id}", 
            $"已停止容器 {container.Name} ({container.Id})");
    }
}

// Commands/RestartCommand.cs - 重启容器（交互式选择）
public class RestartCommand : InteractiveCommandBase
{
    private readonly IContainerService _containerService;
    private readonly IInteractiveService _interactiveService;
    
    public RestartCommand(IContainerService containerService, IInteractiveService interactiveService)
        : base("restart", "重启指定容器（支持交互式选择）")
    {
        var containerArgument = new Argument<string?>("container", "容器名称或ID（可选）");
        AddArgument(containerArgument);
        
        this.SetHandler(HandleAsync, containerArgument);
    }
    
    private async Task HandleAsync(string? containerName)
    {
        if (string.IsNullOrEmpty(containerName))
        {
            var containers = await _containerService.ListProjectRelatedContainersAsync();
            var selectedContainer = await _interactiveService.SelectFromListAsync(
                "请选择要重启的容器：", containers);
                
            await RestartContainerAsync(selectedContainer);
        }
        else
        {
            await RestartContainerAsync(new ContainerInfo { Name = containerName });
        }
    }
}

// Commands/LogsCommand.cs - 查看容器日志（交互式选择）
public class LogsCommand : InteractiveCommandBase
{
    private readonly IContainerService _containerService;
    private readonly IInteractiveService _interactiveService;
    
    public LogsCommand(IContainerService containerService, IInteractiveService interactiveService)
        : base("logs", "查看容器日志（支持交互式选择）")
    {
        var containerArgument = new Argument<string?>("container", "容器名称或ID（可选）");
        AddArgument(containerArgument);
        
        var followOption = new Option<bool>(["-f", "--follow"], "实时显示日志");
        AddOption(followOption);
        
        this.SetHandler(HandleAsync, containerArgument, followOption);
    }
    
    private async Task HandleAsync(string? containerName, bool follow)
    {
        if (string.IsNullOrEmpty(containerName))
        {
            var containers = await _containerService.ListProjectRelatedContainersAsync();
            var selectedContainer = await _interactiveService.SelectFromListAsync(
                "请选择要查看日志的容器：", containers);
                
            await ShowLogsAsync(selectedContainer, follow);
        }
        else
        {
            await ShowLogsAsync(new ContainerInfo { Name = containerName }, follow);
        }
    }
}

// Commands/ShellCommand.cs - 进入容器shell（交互式选择）
public class ShellCommand : InteractiveCommandBase
{
    private readonly IContainerService _containerService;
    private readonly IInteractiveService _interactiveService;
    
    public ShellCommand(IContainerService containerService, IInteractiveService interactiveService)
        : base("shell", "进入容器交互式shell（支持交互式选择）")
    {
        var containerArgument = new Argument<string?>("container", "容器名称或ID（可选）");
        AddArgument(containerArgument);
        
        this.SetHandler(HandleAsync, containerArgument);
    }
    
    private async Task HandleAsync(string? containerName)
    {
        if (string.IsNullOrEmpty(containerName))
        {
            var containers = await _containerService.ListProjectRelatedContainersAsync();
            var runningContainers = containers.Where(c => c.Status == ContainerStatus.Running);
            
            var selectedContainer = await _interactiveService.SelectFromListAsync(
                "请选择要进入的容器：", runningContainers);
                
            await EnterContainerAsync(selectedContainer);
        }
        else
        {
            await EnterContainerAsync(new ContainerInfo { Name = containerName });
        }
    }
}

// Commands/ImagesCommand.cs - 镜像三层统一管理命令
public class ImagesCommand : Command
{
    public ImagesCommand() : base("images", "镜像三层统一管理（Deck配置+Podman镜像+容器）")
    {
        AddCommand(new ImagesListCommand());
        AddCommand(new ImagesCleanCommand());
        AddCommand(new ImagesInfoCommand());
        AddCommand(new ImagesHelpCommand());
    }
}

public class ImagesListCommand : Command
{
    private readonly IImagesUnifiedService _imagesService;
    
    public ImagesListCommand(IImagesUnifiedService imagesService) 
        : base("list", "显示三层内容的统一列表")
    {
        this.SetHandler(HandleAsync);
    }
    
    private async Task HandleAsync()
    {
        var unifiedList = await _imagesService.GetUnifiedResourceListAsync();
        // 显示三层统一列表
        await DisplayUnifiedResourceListAsync(unifiedList);
    }
}

// Commands/DoctorCommand.cs - 系统诊断命令（重要缺失）
public class DoctorCommand : Command
{
    private readonly ISystemDetectionService _systemDetectionService;
    private readonly INetworkService _networkService;
    private readonly IContainerService _containerService;
    private readonly IConsoleDisplay _consoleDisplay;
    
    public DoctorCommand(
        ISystemDetectionService systemDetectionService,
        INetworkService networkService,
        IContainerService containerService,
        IConsoleDisplay consoleDisplay) 
        : base("doctor", "系统诊断和健康检查")
    {
        this.SetHandler(HandleAsync);
    }
    
    private async Task HandleAsync()
    {
        await _consoleDisplay.DisplayHeaderAsync("🔍 Deck 系统诊断");
        
        // 1. 系统要求检查
        await CheckSystemRequirementsAsync();
        
        // 2. 网络连接检查
        await CheckNetworkConnectivityAsync();
        
        // 3. 容器引擎检查
        await CheckContainerEngineAsync();
        
        // 4. 目录结构检查
        await CheckDirectoryStructureAsync();
        
        // 5. 生成诊断报告
        await GenerateDiagnosticReportAsync();
    }
}

// Commands/InstallCommand.cs - 自动安装命令
public class InstallCommand : Command
{
    public InstallCommand() : base("install", "自动安装工具依赖")
    {
        AddCommand(new InstallPodmanCommand());
    }
}

public class InstallPodmanCommand : Command
{
    private readonly IContainerEngineInstaller _installer;
    
    public InstallPodmanCommand(IContainerEngineInstaller installer) 
        : base("podman", "自动安装Podman容器引擎")
    {
        this.SetHandler(HandleAsync);
    }
}

// Commands/PsCommand.cs - 智能容器列表命令  
public class PsCommand : Command
{
    private readonly IContainerService _containerService;
    
    public PsCommand(IContainerService containerService) 
        : base("ps", "显示当前项目相关容器（智能过滤）")
    {
        this.SetHandler(HandleAsync);
    }
    
    private async Task HandleAsync()
    {
        var containers = await _containerService.ListProjectRelatedContainersAsync();
        await DisplayProjectContainersAsync(containers);
    }
}

// Commands/InteractiveCommands.cs - 交互式选择基类
public abstract class InteractiveCommandBase : Command
{
    protected InteractiveCommandBase(string name, string description) : base(name, description)
    {
        // 通用的交互式命令基础设施
    }
    
    protected async Task<T> GetInteractiveSelectionAsync<T>(string prompt, IEnumerable<T> options)
        where T : ISelectableItem
    {
        // 通用交互式选择逻辑
        if (!options.Any())
        {
            Console.WriteLine($"❌ 没有可用的选项");
            return default(T);
        }
        
        return await _interactiveService.SelectFromListAsync(prompt, options);
    }
}
```

### 2. 服务层设计 - 三层统一管理架构

根据最新需求，服务层新增了**三层统一管理**、**交互式选择**和**智能清理**等核心服务，实现比原Shell版本更优秀的用户体验。

#### ImagesUnifiedService - 三层统一管理服务（新增核心服务）

```csharp
public interface IImagesUnifiedService
{
    Task<ThreeLayerOptions> GetThreeLayerOptionsAsync(string? envTypeFilter = null);
    Task<UnifiedResourceList> GetUnifiedResourceListAsync();
    Task<CleaningOptions> GetCleaningOptionsAsync(UnifiedResourceType resourceType, string resourceId);
    Task<bool> ExecuteUnifiedCleanAsync(CleaningSelection selection);
    Task<ResourceInfo> GetResourceInfoAsync(string resourceId);
}

public class ImagesUnifiedService : IImagesUnifiedService
{
    private readonly IContainerService _containerService;
    private readonly ITemplateService _templateService;
    private readonly IConfigurationService _configService;
    private readonly IFileSystemService _fileSystemService;
    
    public async Task<UnifiedResourceList> GetUnifiedResourceListAsync()
    {
        var deckConfigs = await GetDeckImageConfigsAsync();
        var podmanImages = await GetPodmanImagesAsync();
        var containers = await GetRelatedContainersAsync();
        
        return new UnifiedResourceList
        {
            DeckConfigs = deckConfigs,
            PodmanImages = podmanImages,
            RelatedContainers = containers,
            // 实现三层资源的关联映射
            Relationships = BuildResourceRelationships(deckConfigs, podmanImages, containers)
        };
    }
    
    public async Task<CleaningOptions> GetCleaningOptionsAsync(UnifiedResourceType resourceType, string resourceId)
    {
        return resourceType switch
        {
            UnifiedResourceType.DeckConfig => new CleaningOptions
            {
                Options = [
                    new() { Type = CleaningType.ConfigOnly, Description = "仅删除配置目录（保留Podman镜像和容器）" },
                    new() { Type = CleaningType.ConfigAndStopContainers, Description = "删除配置目录+停止相关容器" },
                    new() { Type = CleaningType.ConfigAndAllRelated, Description = "删除配置目录+所有相关容器+对应镜像" }
                ]
            },
            UnifiedResourceType.PodmanImage => new CleaningOptions
            {
                Warning = "⚠️ 警告：将同步删除基于此镜像的所有容器+对应的Deck配置目录",
                Options = [
                    new() { Type = CleaningType.ForceDeleteImageAndAll, Description = "强制删除镜像+所有相关容器+Deck配置" },
                    new() { Type = CleaningType.DeleteImageAndClearCache, Description = "删除镜像+容器+配置+构建缓存（⚠️ 不推荐）" }
                ]
            },
            UnifiedResourceType.Container => new CleaningOptions
            {
                Options = [
                    new() { Type = CleaningType.ContainerOnly, Description = "仅删除容器（保留镜像和Deck配置）" },
                    new() { Type = CleaningType.ContainerAndConfig, Description = "删除容器+清理相关配置" }
                ]
            },
            _ => throw new ArgumentException($"不支持的资源类型: {resourceType}")
        };
    }
}
```

#### InteractiveService - 交互式选择服务（基于通用抽象重新设计）

```csharp
public interface IInteractiveService
{
    // 核心通用选择方法
    Task<SelectionResult> ShowSelectionAsync(
        string title,
        List<SelectionGroup> groups,
        SelectionOptions? options = null);
        
    // 便捷方法（基于通用抽象）
    Task<T?> SelectFromListAsync<T>(string prompt, IEnumerable<T> options) where T : ISelectableItem;
    Task<ThreeLayerSelection> SelectFromThreeLayersAsync(ThreeLayerOptions options);
    Task<CleaningSelection> SelectCleaningOptionsAsync(CleaningOptions options);
    
    // 确认和提示方法
    Task<bool> ConfirmActionAsync(string message, ConfirmationLevel level = ConfirmationLevel.Normal);
    Task DisplayPodmanCommandHintAsync(string equivalentCommand, string description);
}

public interface ICategorizedActionService
{
    Task<TResult> ExecuteActionAsync<TResult>(
        SelectionResult selection, 
        Dictionary<string, object>? context = null);
        
    void RegisterHandler<TResult>(ICategoryHandler<TResult> handler);
    void RegisterHandlers(params object[] handlers);
}

public class InteractiveService : IInteractiveService
{
    private readonly IConsoleDisplay _consoleDisplay;
    private readonly ILogger<InteractiveService> _logger;
    
    public async Task<SelectionResult> ShowSelectionAsync(
        string title,
        List<SelectionGroup> groups,
        SelectionOptions? options = null)
    {
        options ??= new SelectionOptions();
        
        await _consoleDisplay.DisplayHeaderAsync(title);
        
        var allItems = new List<(ISelectableItem Item, string Category, int Index)>();
        var currentIndex = 1;
        
        // 显示所有分组
        foreach (var group in groups)
        {
            if (options.ShowCategoryHeaders && !string.IsNullOrEmpty(group.CategoryDisplayName))
            {
                await _consoleDisplay.DisplaySectionAsync($"\n{group.CategoryDisplayName}");
            }
            
            foreach (var item in group.Items)
            {
                var displayText = options.CustomFormatter?.Invoke(item) ?? item.DisplayName;
                await _consoleDisplay.DisplayItemAsync(currentIndex, displayText);
                
                allItems.Add((item, group.CategoryName, currentIndex));
                currentIndex++;
            }
            
            if (!string.IsNullOrEmpty(group.HintText))
            {
                await _consoleDisplay.DisplayInfoAsync($"💡 {group.HintText}");
            }
        }
        
        // 显示等效命令提示
        if (options.ShowEquivalentCommands)
        {
            await DisplayEquivalentCommandsAsync(groups);
        }
        
        // 获取用户选择
        var selectedIndex = await GetUserInputAsync(options.CancelText);
        
        if (selectedIndex == 0) // 用户取消
        {
            return new SelectionResult { IsCancelled = true };
        }
        
        if (selectedIndex > 0 && selectedIndex <= allItems.Count)
        {
            var (item, category, index) = allItems[selectedIndex - 1];
            return new SelectionResult
            {
                SelectedItem = item,
                Category = category,
                OriginalIndex = index,
                IsCancelled = false
            };
        }
        
        await _consoleDisplay.DisplayErrorAsync("⚠️ 无效的选择，请重新输入");
        return await ShowSelectionAsync(title, groups, options); // 递归重试
    }
    
    public async Task<ThreeLayerSelection> SelectFromThreeLayersAsync(ThreeLayerOptions options)
    {
        // 构建选择分组
        var groups = new List<SelectionGroup>();
        
        // Images 配置组
        if (options.ImagesConfigs.Any())
        {
            groups.Add(new SelectionGroup
            {
                CategoryName = "Images",
                CategoryDisplayName = "Images list:",
                Items = options.ImagesConfigs.Select(c => new ImageConfigItem(c)).Cast<ISelectableItem>().ToList()
            });
        }
        
        // Custom 配置组
        if (options.CustomConfigs.Any())
        {
            groups.Add(new SelectionGroup
            {
                CategoryName = "Custom",
                CategoryDisplayName = "Custom list:",
                Items = options.CustomConfigs.Select(c => new CustomConfigItem(c)).Cast<ISelectableItem>().ToList()
            });
        }
        
        // Templates 配置组
        if (options.TemplatesConfigs.Any())
        {
            groups.Add(new SelectionGroup
            {
                CategoryName = "Templates",
                CategoryDisplayName = "Templates list:",
                Items = options.TemplatesConfigs.Select(t => new TemplateConfigItem(t)).Cast<ISelectableItem>().ToList()
            });
        }
        
        // 使用通用选择方法
        var result = await ShowSelectionAsync("请选择配置类型：", groups);
        
        if (result.IsCancelled || result.SelectedItem == null)
        {
            return new ThreeLayerSelection { IsCancelled = true };
        }
        
        return new ThreeLayerSelection
        {
            ConfigType = Enum.Parse<ConfigurationType>(result.Category),
            ConfigId = result.SelectedItem.Id,
            DisplayName = result.SelectedItem.DisplayName
        };
    }
    
    public async Task DisplayPodmanCommandHintAsync(string equivalentCommand, string description)
    {
        await _consoleDisplay.DisplaySuccessAsync($"✅ {description}");
        await _consoleDisplay.DisplayInfoAsync($"💡 等效的 Podman 命令：{equivalentCommand}");
    }
}
```

#### CleaningService - 智能清理服务（新增核心服务）

```csharp
public interface ICleaningService
{
    Task<MainCleaningOptions> GetMainCleaningOptionsAsync();
    Task<bool> ExecuteMainCleanAsync(string selectedOption);
    Task<UnifiedCleaningOptions> GetImagesUnifiedCleaningOptionsAsync();
    Task<bool> ExecuteImagesUnifiedCleanAsync(UnifiedCleaningSelection selection);
    Task<bool> ExecuteCustomCleanAsync(string configName);
    Task<bool> ExecuteTemplatesCleanAsync(string templateName);
}

public class CleaningService : ICleaningService
{
    private readonly IImagesUnifiedService _imagesUnifiedService;
    private readonly IInteractiveService _interactiveService;
    private readonly IContainerService _containerService;
    private readonly IFileSystemService _fileSystemService;
    
    public async Task<MainCleaningOptions> GetMainCleaningOptionsAsync()
    {
        var imagesConfigs = await GetImagesConfigsAsync();
        var customConfigs = await GetCustomConfigsAsync();
        var templatesConfigs = await GetTemplatesConfigsAsync();
        
        return new MainCleaningOptions
        {
            ImagesConfigs = imagesConfigs.Select((config, index) => 
                new CleaningItem { Index = index + 1, Name = config.Name, Type = "Images" }),
            CustomConfigs = customConfigs.Select((config, index) => 
                new CleaningItem { Index = imagesConfigs.Count + index + 1, Name = config.Name, Type = "Custom" }),
            TemplatesConfigs = templatesConfigs.Select((template, index) => 
                new CleaningItem { Index = imagesConfigs.Count + customConfigs.Count + index + 1, Name = template.Name, Type = "Templates" })
        };
    }
    
    public async Task<bool> ExecuteImagesUnifiedCleanAsync(UnifiedCleaningSelection selection)
    {
        // 根据选择类型和清理策略执行对应的清理操作
        return selection.ResourceType switch
        {
            UnifiedResourceType.DeckConfig => await ExecuteDeckConfigCleanAsync(selection),
            UnifiedResourceType.PodmanImage => await ExecutePodmanImageCleanAsync(selection),
            UnifiedResourceType.Container => await ExecuteContainerCleanAsync(selection),
            _ => throw new ArgumentException($"不支持的资源类型: {selection.ResourceType}")
        };
    }
}
```

#### PortConflictService - 端口冲突检测服务（新增关键缺失服务）

```csharp
public interface IPortConflictService
{
    Task<PortConflictResult> CheckPortConflictsAsync(Dictionary<string, int> ports);
    Task<int> FindAvailablePortAsync(int startPort, int endPort = 65535);
    Task<PortAllocationResult> AutoAllocatePortsAsync(string[] portTypes);
    Task<ProcessInfo> GetPortOccupiedProcessAsync(int port);
    Task<IEnumerable<PortSuggestion>> GetPortConflictSuggestionsAsync(PortConflictResult conflict);
}

public class PortConflictService : IPortConflictService
{
    private readonly ISystemDetectionService _systemDetectionService;
    private readonly ILogger<PortConflictService> _logger;
    
    public async Task<PortConflictResult> CheckPortConflictsAsync(Dictionary<string, int> ports)
    {
        var conflicts = new List<PortConflict>();
        
        foreach (var (portName, portNumber) in ports)
        {
            if (await IsPortInUseAsync(portNumber))
            {
                var process = await GetPortOccupiedProcessAsync(portNumber);
                conflicts.Add(new PortConflict
                {
                    PortName = portName,
                    PortNumber = portNumber,
                    OccupiedBy = process
                });
            }
        }
        
        return new PortConflictResult
        {
            HasConflicts = conflicts.Any(),
            Conflicts = conflicts,
            AvailablePorts = await GetAvailablePortSuggestionsAsync(ports.Keys)
        };
    }
    
    public async Task<ProcessInfo> GetPortOccupiedProcessAsync(int port)
    {
        // 实现跨平台的端口占用进程检测
        var osType = _systemDetectionService.GetOperatingSystemType();
        
        return osType switch
        {
            OperatingSystemType.Windows => await GetPortProcessWindows(port),
            OperatingSystemType.Linux => await GetPortProcessLinux(port),
            OperatingSystemType.MacOS => await GetPortProcessMacOS(port),
            _ => throw new NotSupportedException($"不支持的操作系统: {osType}")
        };
    }
    
    public async Task<int> FindAvailablePortAsync(int startPort, int endPort = 65535)
    {
        for (int port = startPort; port <= endPort; port++)
        {
            if (!await IsPortInUseAsync(port))
                return port;
        }
        
        throw new InvalidOperationException($"在范围 {startPort}-{endPort} 内没有找到可用端口");
    }
    
    public async Task<PortAllocationResult> AutoAllocatePortsAsync(string[] portTypes)
    {
        var allocatedPorts = new Dictionary<string, int>();
        var basePortMap = new Dictionary<string, int>
        {
            ["DEV_PORT"] = 5000,
            ["DEBUG_PORT"] = 9229,
            ["WEB_PORT"] = 8080,
            ["HTTPS_PORT"] = 8443,
            ["ANDROID_DEBUG_PORT"] = 5037
        };
        
        foreach (var portType in portTypes)
        {
            var basePort = basePortMap.GetValueOrDefault(portType, 8000);
            var availablePort = await FindAvailablePortAsync(basePort);
            allocatedPorts[portType] = availablePort;
        }
        
        return new PortAllocationResult
        {
            AllocatedPorts = allocatedPorts,
            Success = true
        };
    }
}
```

#### ImagePermissionService - 镜像权限管理服务（新增关键缺失服务）

```csharp
public interface IImagePermissionService
{
    Task<PermissionCheckResult> CheckFileModificationPermissionAsync(string filePath);
    Task<bool> CanModifyFileAsync(string filePath);
    Task<PermissionExplanation> GetPermissionExplanationAsync(string imagePath);
    Task<ValidationResult> ValidateImageDirectoryNameAsync(string directoryName);
    Task<bool> PreventModificationAsync(string filePath, string reason);
    Task DisplayPermissionWarningAsync(string operation, string filePath);
}

public class ImagePermissionService : IImagePermissionService
{
    private readonly IConsoleDisplay _consoleDisplay;
    private readonly IFileSystemService _fileSystemService;
    private readonly ILogger<ImagePermissionService> _logger;
    
    private readonly HashSet<string> _allowedModificationFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ".env"
    };
    
    private readonly HashSet<string> _protectedFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "compose.yaml", "compose.yml", "docker-compose.yaml", "docker-compose.yml",
        "Dockerfile", "Dockerfile.dev", "Dockerfile.prod",
        "requirements.txt", "package.json", "Cargo.toml", "pubspec.yaml"
    };
    
    public async Task<PermissionCheckResult> CheckFileModificationPermissionAsync(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var isInImagesDirectory = filePath.Contains("/.deck/images/") || filePath.Contains("\\.deck\\images\\");
        
        if (!isInImagesDirectory)
        {
            return new PermissionCheckResult { IsAllowed = true };
        }
        
        var isAllowed = _allowedModificationFiles.Contains(fileName);
        var isProtected = _protectedFiles.Contains(fileName);
        
        return new PermissionCheckResult
        {
            IsAllowed = isAllowed,
            IsProtectedFile = isProtected,
            Reason = isAllowed ? null : $"文件 {fileName} 在镜像目录中受到保护",
            AlternativeAction = GetAlternativeAction(fileName)
        };
    }
    
    public async Task<PermissionExplanation> GetPermissionExplanationAsync(string imagePath)
    {
        return new PermissionExplanation
        {
            AllowedOperations = new[]
            {
                "修改 .env 文件中的运行时变量（如端口配置）",
                "查看所有文件内容",
                "复制镜像目录到 custom 目录进行编辑"
            },
            ProhibitedOperations = new[]
            {
                "修改 compose.yaml - 会破坏镜像配置",
                "修改 Dockerfile - 会影响镜像构建",
                "重命名镜像目录 - 会破坏时间戳追踪",
                "删除核心配置文件 - 会导致启动失败"
            },
            Explanation = "镜像目录是已构建环境的配置快照，只允许修改运行时变量以保证环境一致性。如需大幅修改，请复制到 custom 目录。"
        };
    }
    
    public async Task<bool> PreventModificationAsync(string filePath, string reason)
    {
        await _consoleDisplay.DisplayWarningAsync($"⚠️ 操作被阻止: {reason}");
        await _consoleDisplay.DisplayInfoAsync("💡 如需修改此文件，请使用以下命令将配置复制到可编辑的 custom 目录:");
        await _consoleDisplay.DisplayInfoAsync($"   deck custom create --from-image {Path.GetFileName(Path.GetDirectoryName(filePath))}");
        
        return false;
    }
    
    private string GetAlternativeAction(string fileName)
    {
        return fileName.ToLower() switch
        {
            var f when f.Contains("compose") => "使用 'deck custom create --from-image' 创建可编辑副本",
            "dockerfile" => "复制到 custom 目录后修改",
            _ => "只允许修改 .env 文件中的运行时变量"
        };
    }
}
```

#### NetworkService - 网络检测服务（简化实现 - 专注模板同步）

```csharp
/// <summary>
/// 网络服务 - 专注于模板同步的网络处理
/// 不再进行通用网络测试，只处理实际需要的场景
/// </summary>
public interface INetworkService
{
    /// <summary>
    /// 测试模板仓库连接性 - 仅在实际同步模板时使用
    /// </summary>
    Task<bool> TestTemplateRepositoryAsync(string repositoryUrl, int timeout = 10000);
    
    /// <summary>
    /// 废弃方法 - 保持向后兼容性
    /// </summary>
    [Obsolete("不再进行通用网络测试，只在模板同步时检测仓库连接性")]
    Task<NetworkConnectivityResult> CheckConnectivityAsync(int timeout = 5000);
    
    // 其他接口方法为向后兼容而保留，实际返回默认值
}

public class NetworkService : INetworkService
{
    private readonly ILogger<NetworkService> _logger;
    private readonly HttpClient _httpClient;
    
    /// <summary>
    /// 核心功能：测试模板仓库连接性
    /// 仅用于 doctor 命令和模板同步过程中的连接性检查
    /// </summary>
    public async Task<bool> TestTemplateRepositoryAsync(string repositoryUrl, int timeout = 10000)
    {
        _logger.LogInformation("测试模板仓库连接性: {RepositoryUrl}", repositoryUrl);
        
        try
        {
            using var cts = new CancellationTokenSource(timeout);
            using var request = new HttpRequestMessage(HttpMethod.Head, repositoryUrl);
            request.Headers.Add("User-Agent", "Deck-Template-Sync/1.0");
            
            using var response = await _httpClient.SendAsync(request, cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("模板仓库连接超时: {RepositoryUrl}", repositoryUrl);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "模板仓库连接失败: {RepositoryUrl}", repositoryUrl);
            return false;
        }
    }
    
    /// <summary>
    /// 废弃方法：不再进行通用网络测试
    /// 返回兼容的默认值以保持向后兼容性
    /// </summary>
    [Obsolete("不再进行通用网络测试，只在模板同步时检测仓库连接性")]
    public async Task<NetworkConnectivityResult> CheckConnectivityAsync(int timeout = 5000)
    {
        await Task.CompletedTask;
        _logger.LogWarning("CheckConnectivityAsync 已废弃，不再进行通用网络测试");
        
        return new NetworkConnectivityResult
        {
            CheckTime = DateTime.UtcNow,
            IsConnected = true,
            OverallStatus = ConnectivityStatus.Connected
        };
    }
    
    // 其他方法实现为存根，返回默认值以保持向后兼容性
    // （详细实现已简化，专注于模板同步功能）
}
```

**设计简化说明：**

1. **专注核心功能**：只保留 `TestTemplateRepositoryAsync` 方法，专门用于测试模板仓库连接性
2. **简化责任边界**：不再测试 Docker Hub、GitHub API 等外部服务，这些由 Docker/Podman 和用户网络配置负责
3. **保持向后兼容**：废弃的方法标记为 `[Obsolete]` 但仍然实现，返回默认值
4. **用户指导优化**：当模板同步失败时，提供明确的解决方案指导

#### ContainerService - 容器管理服务

```csharp
public interface IContainerService
{
    Task<ContainerStatus> GetContainerStatusAsync(string containerName);
    Task<bool> StartContainerAsync(string imageName);
    Task<bool> StopContainerAsync(string containerName);
    Task<bool> BuildImageAsync(string imagePath);
    Task<IEnumerable<ContainerInfo>> ListContainersAsync();
    Task<IEnumerable<ContainerInfo>> ListProjectRelatedContainersAsync(); // 新增：智能过滤项目相关容器
    Task<bool> IsEngineAvailableAsync();
    Task<string> DetectContainerEngineAsync();
}

public class ContainerService : IContainerService
{
    private readonly IContainerEngine _containerEngine;
    private readonly ILogger<ContainerService> _logger;
    
    public async Task<ContainerStatus> GetContainerStatusAsync(string containerName)
    {
        // 检查容器状态：运行中/已停止/不存在
        var result = await _containerEngine.ExecuteAsync($"ps -a --filter name=^{containerName}$ --format {{{{.Status}}}}");
        
        if (string.IsNullOrEmpty(result))
            return ContainerStatus.NotExists;
            
        return result.Contains("Up") ? ContainerStatus.Running : ContainerStatus.Stopped;
    }
    
    public async Task<IEnumerable<ContainerInfo>> ListProjectRelatedContainersAsync()
    {
        // 实现智能过滤：仅显示当前项目相关的容器，比 podman ps -a 更智能
        var allContainers = await ListContainersAsync();
        var currentProjectPath = Directory.GetCurrentDirectory();
        var projectName = Path.GetFileName(currentProjectPath);
        
        return allContainers.Where(container => 
            container.Name.Contains(projectName, StringComparison.OrdinalIgnoreCase) ||
            container.Labels.ContainsKey("deck.project") &&
            container.Labels["deck.project"].Equals(projectName, StringComparison.OrdinalIgnoreCase));
    }
}
```

#### TemplateService - 模板管理服务

```csharp
public interface ITemplateService
{
    Task<bool> SyncTemplatesAsync();
    Task<IEnumerable<TemplateInfo>> GetAvailableTemplatesAsync();
    Task<bool> CreateCustomConfigAsync(string templateName, string configName);
    Task<TemplateValidationResult> ValidateTemplateAsync(string templatePath);
}

public class TemplateService : ITemplateService
{
    private readonly IGitService _gitService;
    private readonly IFileSystemService _fileSystemService;
    private readonly IConfigurationService _configService;
    
    public async Task<bool> SyncTemplatesAsync()
    {
        var config = await _configService.GetConfigAsync();
        var templatesPath = Path.Combine(DeckConstants.DeckDirectory, "templates");
        
        try
        {
            await _gitService.CloneOrPullAsync(config.TemplateRepository, templatesPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("模板同步失败: {Error}", ex.Message);
            return false;
        }
    }
}
```

#### ConfigurationService - 配置管理服务

```csharp
public interface IConfigurationService
{
    Task<DeckConfig> GetConfigAsync();
    Task SaveConfigAsync(DeckConfig config);
    Task<ComposeValidationResult> ValidateComposeFileAsync(string filePath);
    Task<Dictionary<string, string>> ParseEnvFileAsync(string filePath);
    Task<string> ReplaceTemplateVariablesAsync(string template, Dictionary<string, string> variables);
}

public class ConfigurationService : IConfigurationService
{
    private readonly IYamlParser _yamlParser;
    private readonly IFileSystemService _fileSystemService;
    
    public async Task<ComposeValidationResult> ValidateComposeFileAsync(string filePath)
    {
        var content = await _fileSystemService.ReadAllTextAsync(filePath);
        
        try
        {
            var compose = _yamlParser.Deserialize<ComposeFile>(content);
            
            var result = new ComposeValidationResult { IsValid = true };
            
            // 验证必需字段
            if (compose.Services == null || !compose.Services.Any())
            {
                result.IsValid = false;
                result.Errors.Add("缺少 services 字段");
            }
            
            return result;
        }
        catch (Exception ex)
        {
            return new ComposeValidationResult 
            { 
                IsValid = false, 
                Errors = { $"YAML解析失败: {ex.Message}" }
            };
        }
    }
}
```

### 3. 基础设施层设计

#### FileSystemService - 文件系统服务

```csharp
public interface IFileSystemService
{
    Task<bool> DirectoryExistsAsync(string path);
    Task CreateDirectoryAsync(string path);
    Task<string> ReadAllTextAsync(string filePath);
    Task WriteAllTextAsync(string filePath, string content);
    Task CopyDirectoryAsync(string sourcePath, string destinationPath);
    Task<IEnumerable<string>> GetDirectoriesAsync(string path);
    Task<FileInfo> GetFileInfoAsync(string filePath);
}

public class FileSystemService : IFileSystemService
{
    public async Task CreateDirectoryAsync(string path)
    {
        await Task.Run(() => Directory.CreateDirectory(path));
    }
    
    public async Task<string> ReadAllTextAsync(string filePath)
    {
        return await File.ReadAllTextAsync(filePath);
    }
    
    public async Task CopyDirectoryAsync(string sourcePath, string destinationPath)
    {
        await Task.Run(() =>
        {
            var sourceDir = new DirectoryInfo(sourcePath);
            var destDir = new DirectoryInfo(destinationPath);
            
            CopyDirectoryRecursive(sourceDir, destDir);
        });
    }
}
```

#### SystemDetectionService - 系统检测服务

```csharp
public interface ISystemDetectionService
{
    Task<SystemInfo> GetSystemInfoAsync();
    Task<bool> CheckSystemRequirementsAsync();
    Task<NetworkStatus> CheckNetworkConnectivityAsync();
    Task<ProjectInfo> DetectProjectEnvironmentAsync(string projectPath);
}

public class SystemDetectionService : ISystemDetectionService
{
    public async Task<SystemInfo> GetSystemInfoAsync()
    {
        return await Task.Run(() =>
        {
            var osInfo = Environment.OSVersion;
            var architecture = RuntimeInformation.ProcessArchitecture;
            var osDescription = RuntimeInformation.OSDescription;
            
            return new SystemInfo
            {
                OperatingSystem = GetOperatingSystemType(),
                Architecture = architecture.ToString(),
                Version = osInfo.Version.ToString(),
                Description = osDescription,
                MemoryGB = GetAvailableMemoryGB(),
                DiskSpaceGB = GetAvailableDiskSpaceGB()
            };
        });
    }
    
    private OperatingSystemType GetOperatingSystemType()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return OperatingSystemType.Windows;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return OperatingSystemType.Linux;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return OperatingSystemType.MacOS;
            
        return OperatingSystemType.Unknown;
    }
}
```

#### ContainerEngine - 容器引擎抽象

```csharp
public interface IContainerEngine
{
    string EngineName { get; }
    Task<bool> IsAvailableAsync();
    Task<string> ExecuteAsync(string command);
    Task<bool> BuildImageAsync(string contextPath, string dockerfilePath, string imageName);
    Task<bool> RunContainerAsync(string imageName, ContainerRunOptions options);
}

public class PodmanEngine : IContainerEngine
{
    public string EngineName => "podman";
    
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var result = await ExecuteAsync("version");
            return !string.IsNullOrEmpty(result);
        }
        catch
        {
            return false;
        }
    }
    
    public async Task<string> ExecuteAsync(string command)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "podman",
            Arguments = command,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        
        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new ContainerEngineException($"Podman命令执行失败: {error}");
        }
        
        return output;
    }
}
```

## 数据模型设计

### 交互式选择通用抽象（新增核心架构）

基于深度分析，多个命令都使用了**分类列表+连续编号+类型判断+差异化逻辑**的交互模式。为此设计通用抽象以实现代码复用和用户体验一致性：

```csharp
// 可选择项目通用接口
public interface ISelectableItem
{
    string Id { get; }
    string DisplayName { get; }
    string Description { get; }
    string Category { get; }
    Dictionary<string, object> Metadata { get; }
}

// 选择分组模型
public class SelectionGroup
{
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryDisplayName { get; set; } = string.Empty;
    public List<ISelectableItem> Items { get; set; } = new();
    public int StartIndex { get; set; }  // 该分组的起始序号
    public string? HintText { get; set; }  // 分组提示信息
}

// 选择结果模型
public class SelectionResult
{
    public ISelectableItem? SelectedItem { get; set; }
    public string Category { get; set; } = string.Empty;
    public int OriginalIndex { get; set; }
    public bool IsCancelled { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
}

// 选择选项配置
public class SelectionOptions
{
    public string CancelText { get; set; } = "按Enter取消";
    public bool ShowEquivalentCommands { get; set; } = true;
    public bool ShowCategoryHeaders { get; set; } = true;
    public Func<ISelectableItem, string>? CustomFormatter { get; set; }
    public bool AllowMultipleSelection { get; set; } = false;
}

// 类型处理器接口（策略模式）
public interface ICategoryHandler<TResult>
{
    string CategoryName { get; }
    Task<TResult> HandleAsync(ISelectableItem item, Dictionary<string, object> context);
    Task<bool> CanHandleAsync(ISelectableItem item);
}

// 具体实现示例
public class ImageConfigItem : ISelectableItem
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = "Images";
    public Dictionary<string, object> Metadata { get; init; } = new();
    
    public ImageConfigItem(ImageConfig config)
    {
        Id = config.Name;
        DisplayName = $"{config.Name} (构建时间: {config.CreatedAt:MM-dd HH:mm})";
        Description = config.Description;
        Metadata["ConfigPath"] = config.Path;
        Metadata["CreatedAt"] = config.CreatedAt;
    }
}

public class ContainerItem : ISelectableItem
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = "Containers";
    public Dictionary<string, object> Metadata { get; init; } = new();
    
    public ContainerItem(ContainerInfo container)
    {
        Id = container.Id;
        DisplayName = $"{container.Name} ({container.Status}) [ID: {container.Id[..8]}]";
        Description = $"基于镜像 {container.ImageName}";
        Metadata["ContainerInfo"] = container;
        Metadata["Status"] = container.Status;
    }
}

public class PodmanImageItem : ISelectableItem
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = "PodmanImages";
    public Dictionary<string, object> Metadata { get; init; } = new();
    
    public PodmanImageItem(PodmanImage image)
    {
        Id = image.Id;
        DisplayName = $"{image.Repository}:{image.Tag} ({image.Size})";
        Description = $"镜像ID: {image.Id[..12]}";
        Metadata["ImageInfo"] = image;
        Metadata["Size"] = image.Size;
    }
}
```

### 核心模型

```csharp
// 系统信息模型
public class SystemInfo
{
    public OperatingSystemType OperatingSystem { get; set; }
    public string Architecture { get; set; }
    public string Version { get; set; }
    public string Description { get; set; }
    public int MemoryGB { get; set; }
    public int DiskSpaceGB { get; set; }
}

// 端口冲突检测模型（新增）
public class PortConflictResult
{
    public bool HasConflicts { get; set; }
    public IEnumerable<PortConflict> Conflicts { get; set; } = [];
    public Dictionary<string, int> AvailablePorts { get; set; } = new();
}

public class PortConflict
{
    public string PortName { get; set; } = string.Empty;
    public int PortNumber { get; set; }
    public ProcessInfo OccupiedBy { get; set; } = new();
}

public class ProcessInfo
{
    public int ProcessId { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string CommandLine { get; set; } = string.Empty;
    public string StopCommand { get; set; } = string.Empty;
}

public class PortAllocationResult
{
    public Dictionary<string, int> AllocatedPorts { get; set; } = new();
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

// 权限管理模型（新增）
public class PermissionCheckResult
{
    public bool IsAllowed { get; set; }
    public bool IsProtectedFile { get; set; }
    public string? Reason { get; set; }
    public string? AlternativeAction { get; set; }
}

public class PermissionExplanation
{
    public IEnumerable<string> AllowedOperations { get; set; } = [];
    public IEnumerable<string> ProhibitedOperations { get; set; } = [];
    public string Explanation { get; set; } = string.Empty;
}

// 网络检测模型（新增）
public class NetworkConnectivityResult
{
    public bool IsConnected { get; set; }
    public bool HasGitHubAccess { get; set; }
    public bool HasRegistryAccess { get; set; }
    public IEnumerable<ConnectivityCheck> Results { get; set; } = [];
}

public class ConnectivityCheck
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public int ResponseTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
}

public class NetworkFallbackStrategy
{
    public string Message { get; set; } = string.Empty;
    public IEnumerable<string> Actions { get; set; } = [];
    public bool ContinueOffline { get; set; }
}

public enum NetworkIssueType
{
    NoInternet,
    GitHubBlocked,
    RegistryBlocked,
    ProxyRequired
}

// 容器信息模型
public class ContainerInfo
{
    public string Name { get; set; }
    public string ImageName { get; set; }
    public ContainerStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, string> Ports { get; set; }
}

// 模板信息模型
public class TemplateInfo
{
    public string Name { get; set; }
    public string Path { get; set; }
    public TemplateType Type { get; set; }
    public bool IsComplete { get; set; }
    public List<string> MissingFiles { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
}

// 项目信息模型
public class ProjectInfo
{
    public ProjectType Type { get; set; }
    public List<string> DetectedFiles { get; set; }
    public string RecommendedTemplate { get; set; }
    public Dictionary<string, string> ProjectMetadata { get; set; }
}

// 配置模型
public class DeckConfig
{
    public TemplateConfig Templates { get; set; }
    public ContainerConfig Container { get; set; }
    public NetworkConfig Network { get; set; }
}

public class TemplateConfig
{
    public RepositoryConfig Repository { get; set; }
    public bool AutoUpdate { get; set; }
    public TimeSpan CacheExpire { get; set; }
}

public class RepositoryConfig
{
    public string Url { get; set; }
    public string Branch { get; set; }
}
```

### 枚举定义

```csharp
public enum OperatingSystemType
{
    Windows,
    Linux,
    MacOS,
    Unknown
}

public enum ContainerStatus
{
    Running,
    Stopped,
    NotExists,
    Error
}

public enum ProjectType
{
    Tauri,
    Flutter,
    Avalonia,
    ReactNative,
    Electron,
    NodeJS,
    Rust,
    DotNet,
    Unknown
}

public enum TemplateType
{
    Official,
    Custom,
    Image
}
```

## 错误处理策略

### 自定义异常体系

```csharp
public abstract class DeckException : Exception
{
    protected DeckException(string message) : base(message) { }
    protected DeckException(string message, Exception innerException) : base(message, innerException) { }
}

public class ContainerEngineException : DeckException
{
    public ContainerEngineException(string message) : base(message) { }
    public ContainerEngineException(string message, Exception innerException) : base(message, innerException) { }
}

public class TemplateException : DeckException
{
    public TemplateException(string message) : base(message) { }
    public TemplateException(string message, Exception innerException) : base(message, innerException) { }
}

public class ConfigurationException : DeckException
{
    public ConfigurationException(string message) : base(message) { }
    public ConfigurationException(string message, Exception innerException) : base(message, innerException) { }
}
```

### 全局错误处理

```csharp
public class GlobalExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    
    public async Task<int> HandleExceptionAsync(Exception exception)
    {
        return exception switch
        {
            ContainerEngineException ex => await HandleContainerEngineExceptionAsync(ex),
            TemplateException ex => await HandleTemplateExceptionAsync(ex),
            ConfigurationException ex => await HandleConfigurationExceptionAsync(ex),
            _ => await HandleGenericExceptionAsync(exception)
        };
    }
    
    private async Task<int> HandleContainerEngineExceptionAsync(ContainerEngineException ex)
    {
        _logger.LogError(ex, "容器引擎错误");
        
        await Console.Error.WriteLineAsync($"❌ 容器引擎错误: {ex.Message}");
        await Console.Error.WriteLineAsync("💡 建议解决方案:");
        await Console.Error.WriteLineAsync("   1. 检查 Podman/Docker 是否正确安装");
        await Console.Error.WriteLineAsync("   2. 运行 'deck doctor' 进行系统诊断");
        
        return 1;
    }
}
```

## 测试策略

### 单元测试结构

```csharp
// Tests/Deck.Services.Tests/ContainerServiceTests.cs
public class ContainerServiceTests
{
    private readonly Mock<IContainerEngine> _mockContainerEngine;
    private readonly Mock<ILogger<ContainerService>> _mockLogger;
    private readonly ContainerService _containerService;
    
    public ContainerServiceTests()
    {
        _mockContainerEngine = new Mock<IContainerEngine>();
        _mockLogger = new Mock<ILogger<ContainerService>>();
        _containerService = new ContainerService(_mockContainerEngine.Object, _mockLogger.Object);
    }
    
    [Fact]
    public async Task GetContainerStatusAsync_WhenContainerRunning_ReturnsRunning()
    {
        // Arrange
        var containerName = "test-container";
        _mockContainerEngine
            .Setup(x => x.ExecuteAsync($"ps -a --filter name=^{containerName}$ --format {{{{.Status}}}}"))
            .ReturnsAsync("Up 5 minutes");
        
        // Act
        var result = await _containerService.GetContainerStatusAsync(containerName);
        
        // Assert
        Assert.Equal(ContainerStatus.Running, result);
    }
}
```

### 集成测试

```csharp
// Tests/Deck.Console.IntegrationTests/StartCommandTests.cs
public class StartCommandTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;
    
    public StartCommandTests(TestApplicationFactory factory)
    {
        _factory = factory;
    }
    
    [Fact]
    public async Task StartCommand_WithValidEnvironment_ReturnsSuccess()
    {
        // Arrange
        var args = new[] { "start", "tauri" };
        
        // Act
        var exitCode = await _factory.RunCommandAsync(args);
        
        // Assert
        Assert.Equal(0, exitCode);
    }
}
```

## AOT发布配置

### 项目文件配置

```xml
<!-- Deck.Console.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <PublishAot>true</PublishAot>
    <InvariantGlobalization>true</InvariantGlobalization>
    <TrimMode>full</TrimMode>
    <SuppressTrimAnalysisWarnings>false</SuppressTrimAnalysisWarnings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="System.CommandLine" Version="2.0.0-beta4.22272.1" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
    <PackageReference Include="YamlDotNet" Version="13.7.1" />
  </ItemGroup>

  <!-- AOT兼容性配置 -->
  <ItemGroup>
    <TrimmerRootDescriptor Include="TrimmerRoots.xml" />
  </ItemGroup>
</Project>
```

### AOT兼容性处理

```csharp
// AOT兼容的JSON序列化配置
[JsonSerializable(typeof(DeckConfig))]
[JsonSerializable(typeof(TemplateInfo))]
[JsonSerializable(typeof(ContainerInfo))]
public partial class DeckJsonContext : JsonSerializerContext
{
}

// 在Program.cs中配置
public static JsonSerializerOptions JsonOptions => new()
{
    TypeInfoResolver = DeckJsonContext.Default,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};
```

## 依赖注入配置

```csharp
// Program.cs
private static IServiceProvider ConfigureServices()
{
    var services = new ServiceCollection();
    
    // 新增核心服务（.NET版本优化特性）
    services.AddSingleton<IImagesUnifiedService, ImagesUnifiedService>();
    services.AddSingleton<IInteractiveService, InteractiveService>();
    services.AddSingleton<ICleaningService, CleaningService>();
    services.AddSingleton<IPackagingService, PackagingService>();
    
    // 新增关键缺失服务（修复设计缺陷）
    services.AddSingleton<IPortConflictService, PortConflictService>();
    services.AddSingleton<IImagePermissionService, ImagePermissionService>();
    services.AddSingleton<INetworkService, NetworkService>();
    
    // 交互式选择通用抽象服务
    services.AddSingleton<ICategorizedActionService, CategorizedActionService>();
    
    // 原有核心服务
    services.AddSingleton<IContainerService, ContainerService>();
    services.AddSingleton<ITemplateService, TemplateService>();
    services.AddSingleton<IConfigurationService, ConfigurationService>();
    services.AddSingleton<ISystemDetectionService, SystemDetectionService>();
    
    // 新增UI和基础设施服务
    services.AddSingleton<IConsoleDisplay, ConsoleDisplay>();
    
    // 原有基础设施服务
    services.AddSingleton<IFileSystemService, FileSystemService>();
    services.AddSingleton<IGitService, GitService>();
    services.AddSingleton<IYamlParser, YamlParser>();
    
    // 容器引擎（工厂模式）
    services.AddSingleton<IContainerEngineFactory, ContainerEngineFactory>();
    services.AddTransient<PodmanEngine>();
    services.AddTransient<DockerEngine>();
    
    // 命令处理器（所有CLI命令）
    services.AddTransient<StartCommand>();
    services.AddTransient<StopCommand>();
    services.AddTransient<RestartCommand>();
    services.AddTransient<LogsCommand>();
    services.AddTransient<ShellCommand>();
    services.AddTransient<ImagesCommand>();
    services.AddTransient<DoctorCommand>();
    services.AddTransient<InstallCommand>();
    services.AddTransient<PsCommand>();
    
    // 注册类型处理器（策略模式）
    services.AddScoped<ICategoryHandler<StartResult>, ImagesStartHandler>();
    services.AddScoped<ICategoryHandler<StartResult>, CustomStartHandler>(); 
    services.AddScoped<ICategoryHandler<StartResult>, TemplatesStartHandler>();
    
    services.AddScoped<ICategoryHandler<CleanResult>, DeckImagesCleanHandler>();
    services.AddScoped<ICategoryHandler<CleanResult>, PodmanImagesCleanHandler>();
    services.AddScoped<ICategoryHandler<CleanResult>, ContainersCleanHandler>();
    
    services.AddScoped<ICategoryHandler<StopResult>, ContainerStopHandler>();
    services.AddScoped<ICategoryHandler<RestartResult>, ContainerRestartHandler>();
    services.AddScoped<ICategoryHandler<LogsResult>, ContainerLogsHandler>();
    services.AddScoped<ICategoryHandler<ShellResult>, ContainerShellHandler>();
    
    // HTTP客户端（网络检测）
    services.AddHttpClient<NetworkService>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(10);
        client.DefaultRequestHeaders.Add("User-Agent", "Deck-CLI/1.0");
    });
    
    // 日志配置（.NET 9 更新）
    services.AddLogging(builder =>
    {
        builder.AddConsole(options =>
        {
            options.FormatterName = "deck";
        });
        builder.SetMinimumLevel(LogLevel.Information);
        
        // 生产环境中降低日志级别
#if !DEBUG
        builder.SetMinimumLevel(LogLevel.Warning);
#endif
    });
    
    // JSON 序列化配置（AOT 兼容）
    services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.TypeInfoResolver = DeckJsonContext.Default;
    });
    
    return services.BuildServiceProvider();
}
```

## 性能优化考虑

### 1. 异步操作
- 所有I/O操作使用异步方法
- 容器引擎调用使用异步执行
- 文件系统操作使用异步API

### 2. 缓存策略
- 系统信息缓存（避免重复检测）
- 模板信息缓存（减少文件系统访问）
- 容器状态缓存（短期缓存避免频繁查询）

### 3. 内存优化
- 使用`IAsyncEnumerable`处理大量数据
- 及时释放资源（using语句）
- 避免不必要的字符串分配

### 4. 启动优化
- AOT编译减少启动时间
- 延迟初始化非关键服务
- 最小化依赖注入容器的构建时间

## 总结

本设计文档提供了完整的.NET 9 Console重构技术架构，实现了以下核心优化：

### 重构亮点

1. **交互式体验优化** - 所有需要参数的命令支持无参数交互式选择，提供比Shell版本更好的用户体验

2. **三层统一管理** - `deck images` 系列命令统一管理Deck配置+Podman镜像+容器，实现智能资源关联管理

3. **智能清理系统** - 不同层级选择对应不同的清理策略和警告机制，防止误操作

4. **标准平台包分发** - 支持MSI/DMG/DEB/RPM标准安装包，无需手动配置PATH

5. **AOT原生性能** - .NET 9 AOT编译，启动迅速，低资源占用，无需安装运行时

6. **Podman命令提示** - 每次操作后显示等效的Podman命令，帮助用户学习和理解

### 架构特色

- **分层架构** - CLI、服务、基础设施、领域模型清晰分层
- **依赖注入** - 全面使用DI容器，提高测试性和可维护性
- **异常处理** - 完善的异常处理机制和错误恢复策略
- **性能优化** - 异步操作、缓存策略、内存优化、启动优化

本设计架构为.NET Console重构提供了清晰的实现路径，确保在保持原有功能的同时，通过新增的优化特性实现超越原Shell版本的用户体验。