using Deck.Core.Interfaces;
using Deck.Services;
using Microsoft.Extensions.Logging;

// 简单测试配置文件生成功能
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<ConfigurationService>();

var configService = new ConfigurationService(logger);

Console.WriteLine("Deck .NET Console Application");
Console.WriteLine("=============================");

// 测试配置文件生成
Console.WriteLine($"配置文件路径: {configService.GetConfigFilePath()}");

if (!configService.ConfigExists())
{
    Console.WriteLine("配置文件不存在，正在创建默认配置...");
    var config = await configService.GetConfigAsync();
    Console.WriteLine("✅ 默认配置已创建");
    Console.WriteLine($"仓库URL: {config.RemoteTemplates.Repository}");
    Console.WriteLine($"仓库分支: {config.RemoteTemplates.Branch}");
    Console.WriteLine($"自动更新: {config.RemoteTemplates.AutoUpdate}");
}
else
{
    Console.WriteLine("配置文件已存在，正在加载...");
    var config = await configService.GetConfigAsync();
    Console.WriteLine("✅ 配置文件加载成功");
    Console.WriteLine($"仓库URL: {config.RemoteTemplates.Repository}");
    Console.WriteLine($"仓库分支: {config.RemoteTemplates.Branch}");
    Console.WriteLine($"自动更新: {config.RemoteTemplates.AutoUpdate}");
}

Console.WriteLine("配置文件功能测试完成！");
