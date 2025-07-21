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

// 获取服务
var configService = host.Services.GetRequiredService<IConfigurationService>();
var loggingService = host.Services.GetRequiredService<ILoggingService>();
var validator = host.Services.GetRequiredService<IConfigurationValidator>();

var logger = loggingService.GetLogger("Deck.Console");

logger.LogInformation("Deck .NET Console Application 启动");

Console.WriteLine("Deck .NET Console Application");
Console.WriteLine("=============================");

// 测试配置文件和日志系统
Console.WriteLine($"配置文件路径: {configService.GetConfigFilePath()}");
logger.LogInformation("配置文件路径: {ConfigPath}", configService.GetConfigFilePath());

if (!configService.ConfigExists())
{
    Console.WriteLine("配置文件不存在，正在创建默认配置...");
    logger.LogInformation("配置文件不存在，创建默认配置");
    
    var config = await configService.GetConfigAsync();
    Console.WriteLine("✅ 默认配置已创建");
    
    // 验证配置
    var validationResult = await validator.ValidateAsync(config);
    if (!validationResult.IsValid)
    {
        logger.LogWarning("配置验证失败: {Errors}", string.Join(", ", validationResult.Errors));
        foreach (var error in validationResult.Errors)
        {
            Console.WriteLine($"❌ {error}");
        }
    }
    
    if (validationResult.Warnings.Count > 0)
    {
        logger.LogWarning("配置警告: {Warnings}", string.Join(", ", validationResult.Warnings));
        foreach (var warning in validationResult.Warnings)
        {
            Console.WriteLine($"⚠️  {warning}");
        }
    }
    
    Console.WriteLine($"仓库URL: {config.RemoteTemplates.Repository}");
    Console.WriteLine($"仓库分支: {config.RemoteTemplates.Branch}");
    Console.WriteLine($"自动更新: {config.RemoteTemplates.AutoUpdate}");
    Console.WriteLine($"缓存TTL: {config.RemoteTemplates.CacheTtl}");
}
else
{
    Console.WriteLine("配置文件已存在，正在加载...");
    logger.LogInformation("加载现有配置文件");
    
    var config = await configService.GetConfigAsync();
    Console.WriteLine("✅ 配置文件加载成功");
    
    // 测试配置验证
    var validationResult = await validator.ValidateAsync(config);
    logger.LogInformation("配置验证完成: Valid={IsValid}, Errors={ErrorCount}, Warnings={WarningCount}", 
        validationResult.IsValid, validationResult.Errors.Count, validationResult.Warnings.Count);
    
    Console.WriteLine($"仓库URL: {config.RemoteTemplates.Repository}");
    Console.WriteLine($"仓库分支: {config.RemoteTemplates.Branch}");
    Console.WriteLine($"自动更新: {config.RemoteTemplates.AutoUpdate}");
    Console.WriteLine($"缓存TTL: {config.RemoteTemplates.CacheTtl}");
}

// 显示日志配置信息
var logConfig = loggingService.GetCurrentConfiguration();
Console.WriteLine($"\n📋 日志配置信息:");
Console.WriteLine($"默认级别: {logConfig.DefaultLevel}");
Console.WriteLine($"控制台颜色: {logConfig.EnableConsoleColors}");
Console.WriteLine($"时间戳: {logConfig.EnableTimestamps}");

logger.LogInformation("配置文件和日志系统功能测试完成");
Console.WriteLine("\n✅ 配置文件和日志系统功能测试完成！");

// 清理资源
await host.StopAsync();
host.Dispose();
