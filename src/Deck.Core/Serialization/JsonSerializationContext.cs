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
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(List<ConfigurationOption>))]
[JsonSerializable(typeof(List<TemplateInfo>))]
[JsonSerializable(typeof(List<RequirementCheck>))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Default,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
public partial class DeckJsonSerializerContext : JsonSerializerContext
{
}