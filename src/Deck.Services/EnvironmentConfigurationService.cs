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

            // 更新服务名称: 匹配 "xxx-dev:" 或 "xxx-test:" 等模式（只在首次构建时匹配）
            // 如果服务名已经是环境后缀（非 -dev），说明已经被更新过，跳过
            content = Regex.Replace(content, @"^(\s*)(\w+)-dev:", $"$1$2-{envSuffix}:", RegexOptions.Multiline);

            // 更新容器名称和主机名
            // 模式1: ${PROJECT_NAME:-xxx}-dev (如 dotnet-deck 模板)
            content = Regex.Replace(content, @"container_name:\s*\$\{PROJECT_NAME[^}]*\}-dev", 
                $"container_name: ${{PROJECT_NAME:-{projectName}}}");
            content = Regex.Replace(content, @"hostname:\s*\$\{PROJECT_NAME[^}]*\}-dev", 
                $"hostname: ${{PROJECT_NAME:-{projectName}}}");

            // 模式2: ${PROJECT_NAME:-xxx}${VARIABLE_NAME} (如 dotnet, ubuntu, avalonia 模板)
            // 这些模板中 container_name 使用变量后缀而非 -dev，但环境类型信息在 .env 中控制
            // 不需要修改 container_name/hostname，因为 PROJECT_NAME 已由 .env 提供

            // 更新命令（如果存在服务名前缀的 command，改为直接使用 bash）
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