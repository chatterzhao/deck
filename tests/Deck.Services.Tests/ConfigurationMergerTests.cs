using Deck.Core.Models;
using Deck.Services;
using FluentAssertions;

namespace Deck.Services.Tests;

public class ConfigurationMergerTests
{
    private readonly ConfigurationMerger _merger;

    public ConfigurationMergerTests()
    {
        _merger = new ConfigurationMerger();
    }

    [Fact]
    public void Merge_WithNullBaseConfig_ShouldThrowArgumentNullException()
    {
        // Arrange
        DeckConfig baseConfig = null!;
        var overrideConfig = new DeckConfig();

        // Act
        var action = () => _merger.Merge(baseConfig, overrideConfig);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("baseConfig");
    }

    [Fact]
    public void Merge_WithNullOverrideConfig_ShouldReturnBaseConfig()
    {
        // Arrange
        var baseConfig = new DeckConfig
        {
            RemoteTemplates = new RemoteTemplatesConfig
            {
                Repository = "https://github.com/test/repo.git",
                Branch = "main"
            }
        };
        DeckConfig overrideConfig = null!;

        // Act
        var result = _merger.Merge(baseConfig, overrideConfig);

        // Assert
        result.Should().BeSameAs(baseConfig);
    }

    [Fact]
    public void Merge_WithSimpleProperties_ShouldOverrideBaseValues()
    {
        // Arrange
        var baseConfig = new DeckConfig
        {
            RemoteTemplates = new RemoteTemplatesConfig
            {
                Repository = "https://github.com/original/repo.git",
                Branch = "main",
                CacheTtl = "24h",
                AutoUpdate = true
            }
        };

        var overrideConfig = new DeckConfig
        {
            RemoteTemplates = new RemoteTemplatesConfig
            {
                Repository = "https://github.com/override/repo.git",
                Branch = "develop"
            }
        };

        // Act
        var result = _merger.Merge(baseConfig, overrideConfig);

        // Assert
        result.RemoteTemplates.Repository.Should().Be("https://github.com/override/repo.git");
        result.RemoteTemplates.Branch.Should().Be("develop");
        result.RemoteTemplates.CacheTtl.Should().Be("24h"); // Should keep base value
        result.RemoteTemplates.AutoUpdate.Should().BeTrue(); // Should keep base value
    }

    [Fact]
    public void Merge_WithPartialOverride_ShouldMergeValues()
    {
        // Arrange
        var baseConfig = new DeckConfig
        {
            RemoteTemplates = new RemoteTemplatesConfig
            {
                Repository = "https://github.com/original/repo.git",
                Branch = "main",
                CacheTtl = "24h",
                AutoUpdate = true
            }
        };

        var overrideConfig = new DeckConfig
        {
            RemoteTemplates = new RemoteTemplatesConfig
            {
                Repository = null!, // 明确设置为null，应该使用base值
                Branch = null!, // 明确设置为null，应该使用base值
                CacheTtl = "48h" // 覆盖值
                // AutoUpdate 使用默认值，应该使用override的默认值
            }
        };

        // Act
        var result = _merger.Merge(baseConfig, overrideConfig);

        // Assert
        result.RemoteTemplates.Repository.Should().Be("https://github.com/original/repo.git"); // From base (override was null)
        result.RemoteTemplates.Branch.Should().Be("main"); // From base (override was null)
        result.RemoteTemplates.CacheTtl.Should().Be("48h"); // From override
        result.RemoteTemplates.AutoUpdate.Should().BeTrue(); // From override (default value)
    }

    [Fact]
    public void Merge_WithNewRemoteTemplatesInOverride_ShouldUseOverrideDefaults()
    {
        // Arrange
        var baseConfig = new DeckConfig
        {
            RemoteTemplates = new RemoteTemplatesConfig
            {
                Repository = "https://github.com/original/repo.git",
                Branch = "main",
                CacheTtl = "24h",
                AutoUpdate = true
            }
        };

        var overrideConfig = new DeckConfig
        {
            // New RemoteTemplates - all properties will have their default values
            RemoteTemplates = new RemoteTemplatesConfig()
        };

        // Act
        var result = _merger.Merge(baseConfig, overrideConfig);

        // Assert
        // When override has default values (not null), override values should be used
        result.RemoteTemplates.Repository.Should().Be("https://github.com/chatterzhao/deck-templates.git"); // Override default
        result.RemoteTemplates.Branch.Should().Be("main"); // Override default
        result.RemoteTemplates.CacheTtl.Should().Be("24h"); // Override default
        result.RemoteTemplates.AutoUpdate.Should().BeTrue(); // Override default
    }

    [Fact]
    public void Merge_WithEmptyStringOverride_ShouldUseEmptyStrings()
    {
        // Arrange
        var baseConfig = new DeckConfig
        {
            RemoteTemplates = new RemoteTemplatesConfig
            {
                Repository = "https://github.com/original/repo.git",
                Branch = "develop",
                CacheTtl = "48h",
                AutoUpdate = false
            }
        };

        var overrideConfig = new DeckConfig
        {
            RemoteTemplates = new RemoteTemplatesConfig
            {
                Repository = "", // Empty string should be used
                Branch = "staging", // Non-empty override
                CacheTtl = null!, // Null should use base value
                AutoUpdate = true // Override value
            }
        };

        // Act
        var result = _merger.Merge(baseConfig, overrideConfig);

        // Assert
        result.RemoteTemplates.Repository.Should().Be(""); // Empty string from override
        result.RemoteTemplates.Branch.Should().Be("staging"); // From override
        result.RemoteTemplates.CacheTtl.Should().Be("48h"); // From base (override was null)
        result.RemoteTemplates.AutoUpdate.Should().BeTrue(); // From override
    }

    [Fact]
    public void Merge_AotCompatibility_ShouldWorkWithJsonSerialization()
    {
        // Arrange - Test AOT compatibility with JSON serialization
        var baseConfig = new DeckConfig
        {
            RemoteTemplates = new RemoteTemplatesConfig
            {
                Repository = "https://github.com/base/repo.git",
                Branch = "main",
                CacheTtl = "24h",
                AutoUpdate = true
            }
        };

        var overrideConfig = new DeckConfig
        {
            RemoteTemplates = new RemoteTemplatesConfig
            {
                Repository = "https://github.com/override/repo.git",
                CacheTtl = "48h"
            }
        };

        // Act - This tests that the type checking and casting works in AOT
        var result = _merger.Merge(baseConfig, overrideConfig);

        // Assert - Verify the merged result can be used with AOT-compatible JSON serialization
        result.Should().NotBeNull();
        result.RemoteTemplates.Should().NotBeNull();
        result.RemoteTemplates.Repository.Should().Be("https://github.com/override/repo.git");
        result.RemoteTemplates.Branch.Should().Be("main"); // From base
        result.RemoteTemplates.CacheTtl.Should().Be("48h"); // From override
        result.RemoteTemplates.AutoUpdate.Should().BeTrue(); // From base
        
        // Verify the type is exactly what we expect (important for AOT)
        result.Should().BeOfType<DeckConfig>();
        result.RemoteTemplates.Should().BeOfType<RemoteTemplatesConfig>();
    }
}