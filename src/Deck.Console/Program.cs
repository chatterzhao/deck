using System.CommandLine;
using Deck.Console.Commands;
using Deck.Core.Interfaces;
using Deck.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// 创建主机并配置服务
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddDeckServicesWithLogging();
    })
    .Build();

// 创建根命令
var rootCommand = CreateRootCommand(host.Services);

// 执行命令
var result = await rootCommand.InvokeAsync(args);

// 清理资源
await host.StopAsync();
host.Dispose();

return result;

static RootCommand CreateRootCommand(IServiceProvider services)
{
    const string ProgramName = "deck";
    const string Version = "1.0.0";
    const string Description = "开发环境容器化工具 - .NET 版本";
    
    var rootCommand = new RootCommand(Description)
    {
        Name = ProgramName
    };
    
    // 添加自定义版本选项 (避免与System.CommandLine内置版本冲突)
    var versionOption = new Option<bool>(
        aliases: ["-V"],
        description: "显示版本信息")
    {
        Arity = ArgumentArity.ZeroOrOne
    };
    rootCommand.AddGlobalOption(versionOption);
    
    // 设置根命令处理器 - 显示帮助信息
    rootCommand.SetHandler((bool showVersion) =>
    {
        if (showVersion)
        {
            Console.WriteLine($"{ProgramName} {Version}");
            return;
        }
        
        // 显示主帮助信息
        ShowMainHelp(ProgramName, Description, Version);
    }, versionOption);
    
    // 添加所有子命令
    AddSubCommands(rootCommand, services);
    
    return rootCommand;
}

static void AddSubCommands(RootCommand rootCommand, IServiceProvider services)
{
    // 添加 start 命令
    var startCommand = new Command("start", "智能启动开发环境")
    {
        new Argument<string?>("env-type") { Description = "环境类型 (可选)", Arity = ArgumentArity.ZeroOrOne }
    };
    startCommand.SetHandler(async (string? envType) =>
    {
        var logger = services.GetRequiredService<ILoggingService>().GetLogger("Deck.Console.Start");
        var startService = services.GetRequiredService<IStartCommandService>();
        
        logger.LogInformation("Start command called with env-type: {EnvType}", envType ?? "auto-detect");
        
        try
        {
            var result = await startService.ExecuteAsync(envType);
            
            if (result.IsSuccess)
            {
                logger.LogInformation("Start command completed successfully for image: {ImageName}", result.ImageName);
            }
            else
            {
                logger.LogError("Start command failed: {ErrorMessage}", result.ErrorMessage);
                Environment.Exit(1);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Start command execution failed");
            Environment.Exit(1);
        }
    }, startCommand.Arguments.Cast<Argument<string?>>().First());
    rootCommand.AddCommand(startCommand);
    
    // 添加 stop 命令
    var stopCommand = new Command("stop", "停止开发环境")
    {
        new Argument<string?>("image-name") { Description = "镜像名称 (可选)", Arity = ArgumentArity.ZeroOrOne }
    };
    stopCommand.SetHandler(async (string? imageName) =>
    {
        var logger = services.GetRequiredService<ILoggingService>().GetLogger("Deck.Console.Stop");
        logger.LogInformation("Stop command called with image-name: {ImageName}", imageName ?? "interactive-select");
        
        var consoleDisplay = services.GetRequiredService<IConsoleDisplay>();
        var interactiveSelection = services.GetRequiredService<IInteractiveSelectionService>();
        var loggingService = services.GetRequiredService<ILoggingService>();
        var directoryManagement = services.GetRequiredService<IDirectoryManagementService>();
        
        var command = new StopCommand(consoleDisplay, interactiveSelection, loggingService, directoryManagement);
        var success = await command.ExecuteAsync(imageName);
        
        if (!success)
        {
            Environment.Exit(1);
        }
    }, stopCommand.Arguments.Cast<Argument<string?>>().First());
    rootCommand.AddCommand(stopCommand);
    
    // 添加 restart 命令
    var restartCommand = new Command("restart", "重启开发环境")
    {
        new Argument<string?>("image-name") { Description = "镜像名称 (可选)", Arity = ArgumentArity.ZeroOrOne }
    };
    restartCommand.SetHandler(async (string? imageName) =>
    {
        var logger = services.GetRequiredService<ILoggingService>().GetLogger("Deck.Console.Restart");
        logger.LogInformation("Restart command called with image-name: {ImageName}", imageName ?? "interactive-select");
        
        var consoleDisplay = services.GetRequiredService<IConsoleDisplay>();
        var interactiveSelection = services.GetRequiredService<IInteractiveSelectionService>();
        var loggingService = services.GetRequiredService<ILoggingService>();
        var directoryManagement = services.GetRequiredService<IDirectoryManagementService>();
        
        var command = new RestartCommand(consoleDisplay, interactiveSelection, loggingService, directoryManagement);
        var success = await command.ExecuteAsync(imageName);
        
        if (!success)
        {
            Environment.Exit(1);
        }
    }, restartCommand.Arguments.Cast<Argument<string?>>().First());
    rootCommand.AddCommand(restartCommand);
    
    // 添加 logs 命令
    var logsCommand = new Command("logs", "查看容器日志")
    {
        new Argument<string?>("image-name") { Description = "镜像名称 (可选)", Arity = ArgumentArity.ZeroOrOne },
        new Option<bool>(["--follow", "-f"], "实时跟踪日志")
    };
    logsCommand.SetHandler(async (string? imageName, bool follow) =>
    {
        var logger = services.GetRequiredService<ILoggingService>().GetLogger("Deck.Console.Logs");
        logger.LogInformation("Logs command called with image-name: {ImageName}, follow: {Follow}", imageName ?? "interactive-select", follow);
        
        var consoleDisplay = services.GetRequiredService<IConsoleDisplay>();
        var interactiveSelection = services.GetRequiredService<IInteractiveSelectionService>();
        var loggingService = services.GetRequiredService<ILoggingService>();
        var directoryManagement = services.GetRequiredService<IDirectoryManagementService>();
        
        var command = new LogsCommand(consoleDisplay, interactiveSelection, loggingService, directoryManagement);
        var success = await command.ExecuteAsync(imageName, follow);
        
        if (!success)
        {
            Environment.Exit(1);
        }
    }, logsCommand.Arguments.Cast<Argument<string?>>().First(), logsCommand.Options.Cast<Option<bool>>().First());
    rootCommand.AddCommand(logsCommand);
    
    // 添加 shell 命令
    var shellCommand = new Command("shell", "进入容器 shell")
    {
        new Argument<string?>("image-name") { Description = "镜像名称 (可选)", Arity = ArgumentArity.ZeroOrOne }
    };
    shellCommand.SetHandler(async (string? imageName) =>
    {
        var logger = services.GetRequiredService<ILoggingService>().GetLogger("Deck.Console.Shell");
        logger.LogInformation("Shell command called with image-name: {ImageName}", imageName ?? "interactive-select");
        
        var consoleDisplay = services.GetRequiredService<IConsoleDisplay>();
        var interactiveSelection = services.GetRequiredService<IInteractiveSelectionService>();
        var loggingService = services.GetRequiredService<ILoggingService>();
        var directoryManagement = services.GetRequiredService<IDirectoryManagementService>();
        
        var command = new ShellCommand(consoleDisplay, interactiveSelection, loggingService, directoryManagement);
        var success = await command.ExecuteAsync(imageName);
        
        if (!success)
        {
            Environment.Exit(1);
        }
    }, shellCommand.Arguments.Cast<Argument<string?>>().First());
    rootCommand.AddCommand(shellCommand);
    
    // 添加 images 命令
    AddImagesCommand(rootCommand, services);
    
    // 添加 custom 命令 (替换原有 config 命令)
    AddCustomCommand(rootCommand, services);
    
    // 添加 templates 命令
    AddTemplatesCommand(rootCommand, services);
    
    // 添加 doctor 命令
    var doctorCommand = new Command("doctor", "系统诊断检查");
    doctorCommand.SetHandler(async () =>
    {
        var logger = services.GetRequiredService<ILoggingService>().GetLogger("Deck.Console.Doctor");
        logger.LogInformation("Doctor command called");
        
        var consoleDisplay = services.GetRequiredService<IConsoleDisplay>();
        var systemDetectionService = services.GetRequiredService<ISystemDetectionService>();
        var networkService = services.GetRequiredService<INetworkService>();
        var loggingService = services.GetRequiredService<ILoggingService>();
        var directoryManagementService = services.GetRequiredService<IDirectoryManagementService>();
        
        var command = new DoctorCommand(consoleDisplay, systemDetectionService, networkService, loggingService, directoryManagementService);
        var success = await command.ExecuteAsync();
        
        if (!success)
        {
            Environment.Exit(1);
        }
    });
    rootCommand.AddCommand(doctorCommand);
    
    // 添加 clean 命令
    var cleanCommand = new Command("clean", "三层配置清理选择")
    {
        new Option<int>(["--keep", "-k"], () => 5, "保留最新镜像数量")
    };
    cleanCommand.SetHandler(async (int keepCount) =>
    {
        var logger = services.GetRequiredService<ILoggingService>().GetLogger("Deck.Console.Clean");
        logger.LogInformation("Clean command called with keep-count: {KeepCount}", keepCount);
        
        var consoleDisplay = services.GetRequiredService<IConsoleDisplay>();
        var interactiveSelection = services.GetRequiredService<IInteractiveSelectionService>();
        var cleaningService = services.GetRequiredService<ICleaningService>();
        var directoryManagement = services.GetRequiredService<IDirectoryManagementService>();
        var loggingService = services.GetRequiredService<ILoggingService>();
        
        var command = new CleanCommand(consoleDisplay, interactiveSelection, cleaningService, directoryManagement, loggingService);
        var success = await command.ExecuteAsync(keepCount);
        
        if (!success)
        {
            Environment.Exit(1);
        }
    }, cleanCommand.Options.Cast<Option<int>>().First());
    rootCommand.AddCommand(cleanCommand);
    
    // 添加 install 命令
    var installCommand = new Command("install", "安装系统组件")
    {
        new Argument<string>("component") { Description = "要安装的组件 (如: podman)" }
    };
    installCommand.SetHandler((string component) =>
    {
        var logger = services.GetRequiredService<ILoggingService>().GetLogger("Deck.Console.Install");
        logger.LogInformation("Install command called with component: {Component}", component);
        Console.WriteLine($"📦 安装组件... ({component})");
        // TODO: 实现 install 命令逻辑
        Console.WriteLine("Install 命令暂未实现");
    }, installCommand.Arguments.Cast<Argument<string>>().First());
    rootCommand.AddCommand(installCommand);
}

static void AddImagesCommand(RootCommand rootCommand, IServiceProvider services)
{
    var imagesCommand = new Command("images", "镜像管理命令");
    
    // images list 子命令
    var listCommand = new Command("list", "列出已构建镜像");
    listCommand.SetHandler(async () =>
    {
        var logger = services.GetRequiredService<ILoggingService>().GetLogger("Deck.Console.Images.List");
        logger.LogInformation("Images list command called");
        
        var consoleDisplay = services.GetRequiredService<IConsoleDisplay>();
        var imagesUnifiedService = services.GetRequiredService<IImagesUnifiedService>();
        var interactiveSelection = services.GetRequiredService<IInteractiveSelectionService>();
        var loggingService = services.GetRequiredService<ILoggingService>();
        
        var command = new ImagesCommand(consoleDisplay, imagesUnifiedService, interactiveSelection, loggingService);
        var success = await command.ExecuteListAsync();
        
        if (!success)
        {
            Environment.Exit(1);
        }
    });
    imagesCommand.AddCommand(listCommand);
    
    // images clean 子命令
    var cleanCommand = new Command("clean", "清理旧镜像")
    {
        new Option<int>(["--keep", "-k"], () => 5, "保留最新镜像数量")
    };
    cleanCommand.SetHandler(async (int keepCount) =>
    {
        var logger = services.GetRequiredService<ILoggingService>().GetLogger("Deck.Console.Images.Clean");
        logger.LogInformation("Images clean command called with keep-count: {KeepCount}", keepCount);
        
        var consoleDisplay = services.GetRequiredService<IConsoleDisplay>();
        var imagesUnifiedService = services.GetRequiredService<IImagesUnifiedService>();
        var interactiveSelection = services.GetRequiredService<IInteractiveSelectionService>();
        var loggingService = services.GetRequiredService<ILoggingService>();
        
        var command = new ImagesCommand(consoleDisplay, imagesUnifiedService, interactiveSelection, loggingService);
        var success = await command.ExecuteCleanAsync(keepCount);
        
        if (!success)
        {
            Environment.Exit(1);
        }
    }, cleanCommand.Options.Cast<Option<int>>().First());
    imagesCommand.AddCommand(cleanCommand);
    
    // images info 子命令
    var infoCommand = new Command("info", "显示镜像详细信息")
    {
        new Argument<string?>("image-name") { Description = "镜像名称 (可选)", Arity = ArgumentArity.ZeroOrOne }
    };
    infoCommand.SetHandler(async (string? imageName) =>
    {
        var logger = services.GetRequiredService<ILoggingService>().GetLogger("Deck.Console.Images.Info");
        logger.LogInformation("Images info command called with image-name: {ImageName}", imageName ?? "interactive-select");
        
        var consoleDisplay = services.GetRequiredService<IConsoleDisplay>();
        var imagesUnifiedService = services.GetRequiredService<IImagesUnifiedService>();
        var interactiveSelection = services.GetRequiredService<IInteractiveSelectionService>();
        var loggingService = services.GetRequiredService<ILoggingService>();
        
        var command = new ImagesCommand(consoleDisplay, imagesUnifiedService, interactiveSelection, loggingService);
        var success = await command.ExecuteInfoAsync(imageName);
        
        if (!success)
        {
            Environment.Exit(1);
        }
    }, infoCommand.Arguments.Cast<Argument<string?>>().First());
    imagesCommand.AddCommand(infoCommand);
    
    // images help 子命令
    var helpCommand = new Command("help", "显示镜像权限帮助");
    helpCommand.SetHandler(async () =>
    {
        var logger = services.GetRequiredService<ILoggingService>().GetLogger("Deck.Console.Images.Help");
        logger.LogInformation("Images help command called");
        
        var consoleDisplay = services.GetRequiredService<IConsoleDisplay>();
        var imagesUnifiedService = services.GetRequiredService<IImagesUnifiedService>();
        var interactiveSelection = services.GetRequiredService<IInteractiveSelectionService>();
        var loggingService = services.GetRequiredService<ILoggingService>();
        
        var command = new ImagesCommand(consoleDisplay, imagesUnifiedService, interactiveSelection, loggingService);
        var success = await command.ExecuteHelpAsync();
        
        if (!success)
        {
            Environment.Exit(1);
        }
    });
    imagesCommand.AddCommand(helpCommand);
    
    rootCommand.AddCommand(imagesCommand);
}

static void AddCustomCommand(RootCommand rootCommand, IServiceProvider services)
{
    var customCommand = new Command("custom", "自定义配置管理命令");
    
    // custom list 子命令
    var listCommand = new Command("list", "列出用户自定义配置");
    listCommand.SetHandler(async () =>
    {
        var logger = services.GetRequiredService<ILoggingService>().GetLogger("Deck.Console.Custom.List");
        logger.LogInformation("Custom list command called");
        
        var consoleDisplay = services.GetRequiredService<IConsoleDisplay>();
        var interactiveSelection = services.GetRequiredService<IInteractiveSelectionService>();
        var directoryManagement = services.GetRequiredService<IDirectoryManagementService>();
        var fileSystem = services.GetRequiredService<IFileSystemService>();
        var loggingService = services.GetRequiredService<ILoggingService>();
        
        var command = new CustomCommand(consoleDisplay, interactiveSelection, directoryManagement, fileSystem, loggingService);
        var success = await command.ExecuteListAsync();
        
        if (!success)
        {
            Environment.Exit(1);
        }
    });
    customCommand.AddCommand(listCommand);
    
    // custom create 子命令
    var createCommand = new Command("create", "创建新的自定义配置")
    {
        new Argument<string?>("config-name") { Description = "配置名称 (可选)", Arity = ArgumentArity.ZeroOrOne },
        new Argument<string?>("env-type") { Description = "环境类型 (可选)", Arity = ArgumentArity.ZeroOrOne }
    };
    createCommand.SetHandler(async (string? configName, string? envType) =>
    {
        var logger = services.GetRequiredService<ILoggingService>().GetLogger("Deck.Console.Custom.Create");
        logger.LogInformation("Custom create command called with config-name: {ConfigName}, env-type: {EnvType}", 
            configName ?? "interactive-input", envType ?? "interactive-select");
        
        var consoleDisplay = services.GetRequiredService<IConsoleDisplay>();
        var interactiveSelection = services.GetRequiredService<IInteractiveSelectionService>();
        var directoryManagement = services.GetRequiredService<IDirectoryManagementService>();
        var fileSystem = services.GetRequiredService<IFileSystemService>();
        var loggingService = services.GetRequiredService<ILoggingService>();
        
        var command = new CustomCommand(consoleDisplay, interactiveSelection, directoryManagement, fileSystem, loggingService);
        var success = await command.ExecuteCreateAsync(configName, envType);
        
        if (!success)
        {
            Environment.Exit(1);
        }
    }, createCommand.Arguments.Cast<Argument<string?>>().First(), createCommand.Arguments.Cast<Argument<string?>>().Last());
    customCommand.AddCommand(createCommand);
    
    // custom edit 子命令
    var editCommand = new Command("edit", "编辑自定义配置")
    {
        new Argument<string?>("config-name") { Description = "配置名称 (可选)", Arity = ArgumentArity.ZeroOrOne }
    };
    editCommand.SetHandler(async (string? configName) =>
    {
        var logger = services.GetRequiredService<ILoggingService>().GetLogger("Deck.Console.Custom.Edit");
        logger.LogInformation("Custom edit command called with config-name: {ConfigName}", configName ?? "interactive-select");
        
        var consoleDisplay = services.GetRequiredService<IConsoleDisplay>();
        var interactiveSelection = services.GetRequiredService<IInteractiveSelectionService>();
        var directoryManagement = services.GetRequiredService<IDirectoryManagementService>();
        var fileSystem = services.GetRequiredService<IFileSystemService>();
        var loggingService = services.GetRequiredService<ILoggingService>();
        
        var command = new CustomCommand(consoleDisplay, interactiveSelection, directoryManagement, fileSystem, loggingService);
        var success = await command.ExecuteEditAsync(configName);
        
        if (!success)
        {
            Environment.Exit(1);
        }
    }, editCommand.Arguments.Cast<Argument<string?>>().First());
    customCommand.AddCommand(editCommand);
    
    // custom clean 子命令
    var cleanCommand = new Command("clean", "清理自定义配置");
    cleanCommand.SetHandler(async () =>
    {
        var logger = services.GetRequiredService<ILoggingService>().GetLogger("Deck.Console.Custom.Clean");
        logger.LogInformation("Custom clean command called");
        
        var consoleDisplay = services.GetRequiredService<IConsoleDisplay>();
        var interactiveSelection = services.GetRequiredService<IInteractiveSelectionService>();
        var directoryManagement = services.GetRequiredService<IDirectoryManagementService>();
        var fileSystem = services.GetRequiredService<IFileSystemService>();
        var loggingService = services.GetRequiredService<ILoggingService>();
        
        var command = new CustomCommand(consoleDisplay, interactiveSelection, directoryManagement, fileSystem, loggingService);
        var success = await command.ExecuteCleanAsync();
        
        if (!success)
        {
            Environment.Exit(1);
        }
    });
    customCommand.AddCommand(cleanCommand);
    
    rootCommand.AddCommand(customCommand);
}

static void AddTemplatesCommand(RootCommand rootCommand, IServiceProvider services)
{
    var templatesCommand = new Command("templates", "模板管理命令");
    
    // templates list 子命令
    var listCommand = new Command("list", "列出可用模板");
    listCommand.SetHandler(async () =>
    {
        var logger = services.GetRequiredService<ILoggingService>().GetLogger("Deck.Console.Templates.List");
        logger.LogInformation("Templates list command called");
        
        var consoleDisplay = services.GetRequiredService<IConsoleDisplay>();
        var interactiveSelection = services.GetRequiredService<IInteractiveSelectionService>();
        var directoryManagement = services.GetRequiredService<IDirectoryManagementService>();
        var configurationService = services.GetRequiredService<IConfigurationService>();
        var networkService = services.GetRequiredService<INetworkService>();
        var loggingService = services.GetRequiredService<ILoggingService>();
        
        var command = new TemplatesCommand(consoleDisplay, interactiveSelection, directoryManagement, configurationService, networkService, loggingService);
        var success = await command.ExecuteListAsync();
        
        if (!success)
        {
            Environment.Exit(1);
        }
    });
    templatesCommand.AddCommand(listCommand);
    
    // templates update 子命令
    var updateCommand = new Command("update", "更新远程模板");
    updateCommand.SetHandler(async () =>
    {
        var logger = services.GetRequiredService<ILoggingService>().GetLogger("Deck.Console.Templates.Update");
        logger.LogInformation("Templates update command called");
        
        var consoleDisplay = services.GetRequiredService<IConsoleDisplay>();
        var interactiveSelection = services.GetRequiredService<IInteractiveSelectionService>();
        var directoryManagement = services.GetRequiredService<IDirectoryManagementService>();
        var configurationService = services.GetRequiredService<IConfigurationService>();
        var networkService = services.GetRequiredService<INetworkService>();
        var loggingService = services.GetRequiredService<ILoggingService>();
        
        var command = new TemplatesCommand(consoleDisplay, interactiveSelection, directoryManagement, configurationService, networkService, loggingService);
        var success = await command.ExecuteUpdateAsync();
        
        if (!success)
        {
            Environment.Exit(1);
        }
    });
    templatesCommand.AddCommand(updateCommand);
    
    // templates config 子命令
    var configCommand = new Command("config", "显示模板配置");
    configCommand.SetHandler(async () =>
    {
        var logger = services.GetRequiredService<ILoggingService>().GetLogger("Deck.Console.Templates.Config");
        logger.LogInformation("Templates config command called");
        
        var consoleDisplay = services.GetRequiredService<IConsoleDisplay>();
        var interactiveSelection = services.GetRequiredService<IInteractiveSelectionService>();
        var directoryManagement = services.GetRequiredService<IDirectoryManagementService>();
        var configurationService = services.GetRequiredService<IConfigurationService>();
        var networkService = services.GetRequiredService<INetworkService>();
        var loggingService = services.GetRequiredService<ILoggingService>();
        
        var command = new TemplatesCommand(consoleDisplay, interactiveSelection, directoryManagement, configurationService, networkService, loggingService);
        var success = await command.ExecuteConfigAsync();
        
        if (!success)
        {
            Environment.Exit(1);
        }
    });
    templatesCommand.AddCommand(configCommand);
    
    // templates sync 子命令
    var syncCommand = new Command("sync", "手动同步模板到项目");
    syncCommand.SetHandler(async () =>
    {
        var logger = services.GetRequiredService<ILoggingService>().GetLogger("Deck.Console.Templates.Sync");
        logger.LogInformation("Templates sync command called");
        
        var consoleDisplay = services.GetRequiredService<IConsoleDisplay>();
        var interactiveSelection = services.GetRequiredService<IInteractiveSelectionService>();
        var directoryManagement = services.GetRequiredService<IDirectoryManagementService>();
        var configurationService = services.GetRequiredService<IConfigurationService>();
        var networkService = services.GetRequiredService<INetworkService>();
        var loggingService = services.GetRequiredService<ILoggingService>();
        
        var command = new TemplatesCommand(consoleDisplay, interactiveSelection, directoryManagement, configurationService, networkService, loggingService);
        var success = await command.ExecuteSyncAsync();
        
        if (!success)
        {
            Environment.Exit(1);
        }
    });
    templatesCommand.AddCommand(syncCommand);
    
    // templates clean 子命令
    var cleanCommand = new Command("clean", "清理模板 (不推荐)");
    cleanCommand.SetHandler(async () =>
    {
        var logger = services.GetRequiredService<ILoggingService>().GetLogger("Deck.Console.Templates.Clean");
        logger.LogInformation("Templates clean command called");
        
        var consoleDisplay = services.GetRequiredService<IConsoleDisplay>();
        var interactiveSelection = services.GetRequiredService<IInteractiveSelectionService>();
        var directoryManagement = services.GetRequiredService<IDirectoryManagementService>();
        var configurationService = services.GetRequiredService<IConfigurationService>();
        var networkService = services.GetRequiredService<INetworkService>();
        var loggingService = services.GetRequiredService<ILoggingService>();
        
        var command = new TemplatesCommand(consoleDisplay, interactiveSelection, directoryManagement, configurationService, networkService, loggingService);
        var success = await command.ExecuteCleanAsync();
        
        if (!success)
        {
            Environment.Exit(1);
        }
    });
    templatesCommand.AddCommand(cleanCommand);
    
    rootCommand.AddCommand(templatesCommand);
}

static void ShowMainHelp(string programName, string description, string version)
{
    Console.WriteLine($"{programName} - {description}");
    Console.WriteLine();
    Console.WriteLine($"用法: {programName} <command> [options]");
    Console.WriteLine();
    Console.WriteLine("命令:");
    Console.WriteLine("  start [env-type]      智能启动开发环境");
    Console.WriteLine("  stop [image-name]     停止环境");
    Console.WriteLine("  restart [image-name]  重启环境");
    Console.WriteLine("  logs [image-name]     查看日志");
    Console.WriteLine("  shell [image-name]    进入容器");
    Console.WriteLine();
    Console.WriteLine("  custom list           列出用户自定义配置");
    Console.WriteLine("  custom create         创建新的自定义配置");
    Console.WriteLine("  custom edit           编辑自定义配置");
    Console.WriteLine("  custom clean          清理自定义配置");
    Console.WriteLine();
    Console.WriteLine("  images list           列出已构建镜像");
    Console.WriteLine("  images clean          清理旧镜像");
    Console.WriteLine("  images info           显示镜像信息");
    Console.WriteLine("  images help           显示镜像目录权限说明");
    Console.WriteLine();
    Console.WriteLine("  templates list        列出可用模板");
    Console.WriteLine("  templates update      更新远程模板");
    Console.WriteLine("  templates config      显示模板配置");
    Console.WriteLine("  templates sync        手动同步模板");
    Console.WriteLine("  templates clean       清理模板 (不推荐)");
    Console.WriteLine();
    Console.WriteLine("  doctor                系统诊断");
    Console.WriteLine("  clean                 三层配置清理选择");
    Console.WriteLine("  install podman        安装 Podman");
    Console.WriteLine();
    Console.WriteLine("  help                  显示帮助信息");
    Console.WriteLine("  version               显示版本信息");
    Console.WriteLine();
    Console.WriteLine("示例:");
    Console.WriteLine($"  {programName} start                    # 自动检测并启动");
    Console.WriteLine($"  {programName} start tauri              # 启动 Tauri 环境");
    Console.WriteLine($"  {programName} stop my-app-20241215     # 停止指定镜像");
    Console.WriteLine($"  {programName} logs -f                  # 实时查看日志");
    Console.WriteLine($"  {programName} custom create tauri-dev  # 创建自定义配置");
    Console.WriteLine();
    Console.WriteLine("更多信息请访问: https://github.com/your-org/deck-tool");
    Console.WriteLine();
    Console.WriteLine($"版本: {version}");
}
