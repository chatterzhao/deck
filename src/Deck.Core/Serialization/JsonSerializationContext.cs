using System.Text.Json.Serialization;
using Deck.Core.Models;

namespace Deck.Core.Serialization;

/// <summary>
/// JSON 序列化上下文 - 为 AOT 编译提供静态类型信息
/// 支持 .NET 9 源生成器，避免运行时反射
/// </summary>
[JsonSerializable(typeof(DeckConfig))]
[JsonSerializable(typeof(RemoteTemplatesConfig))]
[JsonSerializable(typeof(SystemInfo))]
[JsonSerializable(typeof(ContainerEngineInfo))]
[JsonSerializable(typeof(ProjectTypeInfo))]
[JsonSerializable(typeof(SystemRequirementsResult))]
[JsonSerializable(typeof(ThreeLayerOptions))]
[JsonSerializable(typeof(ConfigurationOption))]
[JsonSerializable(typeof(ImageMetadata))]
[JsonSerializable(typeof(TemplateInfo))]
[JsonSerializable(typeof(SyncResult))]
[JsonSerializable(typeof(UpdateCheckResult))]
[JsonSerializable(typeof(ContainerInfo))]
[JsonSerializable(typeof(MountInfo))]
[JsonSerializable(typeof(NetworkInfo))]
[JsonSerializable(typeof(PortMapping))]
[JsonSerializable(typeof(ContainerResourceUsage))]
[JsonSerializable(typeof(ContainerListOptions))]
[JsonSerializable(typeof(ComposeValidationResult))]
[JsonSerializable(typeof(ComposeValidationError))]
[JsonSerializable(typeof(ComposeValidationWarning))]
[JsonSerializable(typeof(ServiceValidationResult))]
[JsonSerializable(typeof(ImageValidationResult))]
[JsonSerializable(typeof(PortValidationResult))]
[JsonSerializable(typeof(VolumeValidationResult))]
[JsonSerializable(typeof(EnvironmentValidationResult))]
[JsonSerializable(typeof(ComposeNetworkValidationResult))]
[JsonSerializable(typeof(HealthCheckValidationResult))]
[JsonSerializable(typeof(PortConflictResult))]
[JsonSerializable(typeof(DependencyValidationResult))]
[JsonSerializable(typeof(SecurityScanResult))]
[JsonSerializable(typeof(ComposeValidationSummary))]
[JsonSerializable(typeof(PortCheckResult))]
[JsonSerializable(typeof(PortConflictInfo))]
[JsonSerializable(typeof(ProcessInfo))]
[JsonSerializable(typeof(ProjectPortAllocation))]
[JsonSerializable(typeof(PortResolutionSuggestion))]
[JsonSerializable(typeof(ProcessStopResult))]
[JsonSerializable(typeof(SystemPortUsage))]
[JsonSerializable(typeof(PortUsageInfo))]
[JsonSerializable(typeof(PortUsageStatistics))]
[JsonSerializable(typeof(PortValidationResult))]
[JsonSerializable(typeof(ComposePortValidationResult))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(List<ConfigurationOption>))]
[JsonSerializable(typeof(List<TemplateInfo>))]
[JsonSerializable(typeof(List<RequirementCheck>))]
[JsonSerializable(typeof(List<ContainerInfo>))]
[JsonSerializable(typeof(List<MountInfo>))]
[JsonSerializable(typeof(List<NetworkInfo>))]
[JsonSerializable(typeof(List<PortMapping>))]
[JsonSerializable(typeof(List<ComposeValidationError>))]
[JsonSerializable(typeof(List<ComposeValidationWarning>))]
[JsonSerializable(typeof(List<ServiceValidationResult>))]
[JsonSerializable(typeof(List<ComposePortValidationResult>))]
[JsonSerializable(typeof(List<VolumeValidationResult>))]
[JsonSerializable(typeof(List<EnvironmentValidationResult>))]
[JsonSerializable(typeof(List<PortCheckResult>))]
[JsonSerializable(typeof(List<PortResolutionSuggestion>))]
[JsonSerializable(typeof(List<PortUsageInfo>))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Default,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
public partial class DeckJsonSerializerContext : JsonSerializerContext
{
}