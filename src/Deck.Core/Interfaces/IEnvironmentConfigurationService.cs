using Deck.Core.Models;

namespace Deck.Core.Interfaces;

/// <summary>
/// 环境配置服务接口
/// </summary>
public interface IEnvironmentConfigurationService
{
    /// <summary>
    /// 更新 compose.yaml 文件的环境相关配置
    /// </summary>
    /// <param name="composeFilePath">compose.yaml 文件路径</param>
    /// <param name="environment">环境类型</param>
    /// <param name="projectName">项目名称</param>
    /// <returns>是否更新成功</returns>
    Task<bool> UpdateComposeEnvironmentAsync(string composeFilePath, EnvironmentType environment, string projectName);

    /// <summary>
    /// 更新 .env 文件的环境变量
    /// </summary>
    /// <param name="envFilePath">.env 文件路径</param>
    /// <param name="environment">环境类型</param>
    /// <returns>是否更新成功</returns>
    Task<bool> UpdateEnvFileEnvironmentAsync(string envFilePath, EnvironmentType environment);

    /// <summary>
    /// 计算环境相关的端口配置
    /// </summary>
    /// <param name="basePort">基础端口</param>
    /// <param name="environment">环境类型</param>
    /// <returns>调整后的端口</returns>
    int CalculateEnvironmentPort(int basePort, EnvironmentType environment);
}