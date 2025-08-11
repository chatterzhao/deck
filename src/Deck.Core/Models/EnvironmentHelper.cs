namespace Deck.Core.Models;

/// <summary>
/// 环境配置帮助类
/// </summary>
public static class EnvironmentHelper
{
    /// <summary>
    /// 获取环境配置选项
    /// </summary>
    public static List<EnvironmentOption> GetEnvironmentOptions()
    {
        return new List<EnvironmentOption>
        {
            new EnvironmentOption
            {
                Type = EnvironmentType.Development,
                DisplayName = "开发环境",
                ContainerSuffix = "dev",
                PortOffset = 0,
                EnvironmentValue = "Development",
                IsProduction = false
            },
            new EnvironmentOption
            {
                Type = EnvironmentType.Test,
                DisplayName = "测试环境",
                ContainerSuffix = "test",
                PortOffset = 1000,
                EnvironmentValue = "Test",
                IsProduction = false
            },
            new EnvironmentOption
            {
                Type = EnvironmentType.Production,
                DisplayName = "生产环境",
                ContainerSuffix = "prod",
                PortOffset = 2000,
                EnvironmentValue = "Production",
                IsProduction = true
            }
        };
    }

    /// <summary>
    /// 根据环境类型获取配置选项
    /// </summary>
    public static EnvironmentOption GetEnvironmentOption(EnvironmentType environmentType)
    {
        return GetEnvironmentOptions().First(e => e.Type == environmentType);
    }

    /// <summary>
    /// 获取容器名称
    /// </summary>
    public static string GetContainerName(string baseName, EnvironmentType environment)
    {
        var envOption = GetEnvironmentOption(environment);
        
        // 检查 baseName 是否已经以环境后缀结尾，避免重复添加
        if (baseName.EndsWith($"-{envOption.ContainerSuffix}"))
        {
            return baseName; // 已经包含环境后缀，直接返回
        }
        
        return $"{baseName}-{envOption.ContainerSuffix}";
    }

    /// <summary>
    /// 计算环境相关的端口
    /// </summary>
    public static int CalculatePort(int basePort, EnvironmentType environment)
    {
        var envOption = GetEnvironmentOption(environment);
        return basePort + envOption.PortOffset;
    }
}