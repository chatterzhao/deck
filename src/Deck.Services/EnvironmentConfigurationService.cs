using System.Text.RegularExpressions;
using Deck.Core.Interfaces;
using Deck.Core.Models;
using Microsoft.Extensions.Logging;

namespace Deck.Services;

/// <summary>
/// 环境配置服务实现
/// </summary>
public class EnvironmentConfigurationService : IEnvironmentConfigurationService
{
    private readonly ILogger<EnvironmentConfigurationService> _logger;

    public EnvironmentConfigurationService(ILogger<EnvironmentConfigurationService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> UpdateComposeEnvironmentAsync(string composeFilePath, EnvironmentType environment, string projectName)
    {
        try
        {
            if (!File.Exists(composeFilePath))
            {
                _logger.LogWarning("Compose file not found: {FilePath}", composeFilePath);
                return false;
            }

            var content = await File.ReadAllTextAsync(composeFilePath);
            var envOption = EnvironmentHelper.GetEnvironmentOption(environment);
            var envSuffix = envOption.ContainerSuffix;

            // 不再需要提取基础服务名，命令直接使用bash

            // 更新服务名称 (第一个匹配的服务名)
            content = Regex.Replace(content, @"^\s*(\w+)-dev:", $"  $1-{envSuffix}:", RegexOptions.Multiline);
            
            // 更新容器名称
            content = Regex.Replace(content, @"container_name:\s*\$\{PROJECT_NAME[^}]*\}-dev", 
                $"container_name: ${{PROJECT_NAME:-{projectName}}}-{envSuffix}");

            // 更新主机名
            content = Regex.Replace(content, @"hostname:\s*\$\{PROJECT_NAME[^}]*\}-dev", 
                $"hostname: ${{PROJECT_NAME:-{projectName}}}-{envSuffix}");

            // 更新命令（直接使用bash，不需要服务名前缀）
            content = Regex.Replace(content, @"command:\s*\w+-dev\s+bash", "command: bash");

            await File.WriteAllTextAsync(composeFilePath, content);
            _logger.LogInformation("Updated compose file for environment: {Environment}", environment);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update compose file: {FilePath}", composeFilePath);
            return false;
        }
    }


    public async Task<bool> UpdateEnvFileEnvironmentAsync(string envFilePath, EnvironmentType environment)
    {
        try
        {
            if (!File.Exists(envFilePath))
            {
                _logger.LogWarning("Env file not found: {FilePath}", envFilePath);
                return false;
            }

            var lines = await File.ReadAllLinesAsync(envFilePath);
            var envOption = EnvironmentHelper.GetEnvironmentOption(environment);

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // 更新环境变量
                if (line.StartsWith("DOTNET_ENVIRONMENT=") || line.StartsWith("#DOTNET_ENVIRONMENT="))
                {
                    lines[i] = $"DOTNET_ENVIRONMENT={envOption.EnvironmentValue}";
                }
                else if (line.StartsWith("ASPNETCORE_ENVIRONMENT=") || line.StartsWith("#ASPNETCORE_ENVIRONMENT="))
                {
                    lines[i] = $"ASPNETCORE_ENVIRONMENT={envOption.EnvironmentValue}";
                }
                // 更新端口偏移
                else if (Regex.IsMatch(line, @"^(DEV_PORT|DEBUG_PORT|WEB_PORT|HTTPS_PORT|ANDROID_DEBUG_PORT)=\d+"))
                {
                    var match = Regex.Match(line, @"^(\w+)=(\d+)");
                    if (match.Success)
                    {
                        var portName = match.Groups[1].Value;
                        var originalPort = int.Parse(match.Groups[2].Value);
                        var adjustedPort = CalculateEnvironmentPort(originalPort, environment);
                        lines[i] = $"{portName}={adjustedPort}";
                    }
                }
            }

            await File.WriteAllLinesAsync(envFilePath, lines);
            _logger.LogInformation("Updated env file for environment: {Environment}", environment);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update env file: {FilePath}", envFilePath);
            return false;
        }
    }

    public int CalculateEnvironmentPort(int basePort, EnvironmentType environment)
    {
        return EnvironmentHelper.CalculatePort(basePort, environment);
    }
}