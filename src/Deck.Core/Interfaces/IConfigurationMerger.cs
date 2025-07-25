namespace Deck.Core.Interfaces;

/// <summary>
/// 配置合并服务接口
/// 实现基础配置与覆盖配置的合并功能
/// </summary>
public interface IConfigurationMerger
{
    /// <summary>
    /// 合并基础配置和覆盖配置
    /// </summary>
    /// <typeparam name="T">配置类型</typeparam>
    /// <param name="baseConfig">基础配置</param>
    /// <param name="overrideConfig">覆盖配置</param>
    /// <returns>合并后的配置</returns>
    T Merge<T>(T baseConfig, T overrideConfig) where T : class;
}