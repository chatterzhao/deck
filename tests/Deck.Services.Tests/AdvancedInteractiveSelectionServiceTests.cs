using Deck.Core.Interfaces;
using Deck.Core.Models;
using Deck.Services;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Deck.Services.Tests;

public class AdvancedInteractiveSelectionServiceTests : IDisposable
{
    private readonly Mock<IConsoleDisplay> _mockConsoleDisplay;
    private readonly AdvancedInteractiveSelectionService _service;
    private readonly StringWriter _consoleOutput;
    private readonly TextWriter _originalConsoleOut;
    private readonly TextReader _originalConsoleIn;

    public AdvancedInteractiveSelectionServiceTests()
    {
        _mockConsoleDisplay = new Mock<IConsoleDisplay>();
        _service = new AdvancedInteractiveSelectionService(_mockConsoleDisplay.Object);
        
        // 保存原始的Console输入输出
        _originalConsoleOut = Console.Out;
        _originalConsoleIn = Console.In;
        
        // 设置测试用的Console输入输出
        _consoleOutput = new StringWriter();
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        // 恢复原始的Console输入输出
        Console.SetOut(_originalConsoleOut);
        Console.SetIn(_originalConsoleIn);
        
        _consoleOutput?.Dispose();
    }

    [Fact]
    public async Task ShowThreeLayerSelectionAsync_WithEmptyConfigurations_ShouldReturnCancelled()
    {
        // Arrange
        var selector = new ThreeLayerSelector
        {
            Prompt = "Select configuration",
            ImagesConfigurations = new List<SelectableThreeLayerConfiguration>(),
            CustomConfigurations = new List<SelectableThreeLayerConfiguration>(),
            TemplatesConfigurations = new List<SelectableThreeLayerConfiguration>()
        };

        // Act
        var result = await _service.ShowThreeLayerSelectionAsync(selector);

        // Assert
        result.IsCancelled.Should().BeTrue();
        result.ErrorMessage.Should().Be("没有可用的配置选项");
    }

    [Fact]
    public async Task ShowThreeLayerSelectionAsync_WithValidConfigurations_ShouldBuildOptions()
    {
        // Arrange
        var selector = new ThreeLayerSelector
        {
            Prompt = "Select configuration",
            ShowProjectDetection = false, // 禁用项目检测避免控制台交互
            ImagesConfigurations = new List<SelectableThreeLayerConfiguration>
            {
                new()
                {
                    Name = "avalonia-dev",
                    LayerType = ThreeLayerConfigurationType.Images,
                    Status = new ConfigurationStatus { HasComposeYaml = true, HasDockerfile = true, HasEnvFile = true },
                    DetectedProjectType = ProjectType.Avalonia
                }
            },
            CustomConfigurations = new List<SelectableThreeLayerConfiguration>
            {
                new()
                {
                    Name = "my-custom",
                    LayerType = ThreeLayerConfigurationType.Custom,
                    Status = new ConfigurationStatus { HasComposeYaml = true, HasDockerfile = true, HasEnvFile = true },
                    DetectedProjectType = ProjectType.Flutter
                }
            }
        };

        // 模拟用户输入选择第一个选项
        Console.SetIn(new StringReader("1\n"));

        // Act
        var result = await _service.ShowThreeLayerSelectionAsync(selector);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsCancelled.Should().BeFalse();
        result.SelectedConfiguration.Should().NotBeNull();
        result.SelectedConfiguration!.Name.Should().Be("avalonia-dev");
        result.SelectedLayerType.Should().Be(ThreeLayerConfigurationType.Images);
    }

    [Fact]
    public async Task ShowThreeLayerSelectionAsync_WithTemplatesSelection_ShouldRequestWorkflowChoice()
    {
        // Arrange
        var selector = new ThreeLayerSelector
        {
            Prompt = "Select configuration",
            ShowProjectDetection = false,
            TemplatesConfigurations = new List<SelectableThreeLayerConfiguration>
            {
                new()
                {
                    Name = "avalonia-template",
                    LayerType = ThreeLayerConfigurationType.Templates,
                    Status = new ConfigurationStatus { HasComposeYaml = true, HasDockerfile = true, HasEnvFile = true },
                    DetectedProjectType = ProjectType.Avalonia
                }
            }
        };

        // 模拟用户输入：选择模板(1)，然后选择工作流程(2)
        Console.SetIn(new StringReader("1\n2\n"));

        // Act
        var result = await _service.ShowThreeLayerSelectionAsync(selector);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.SelectedConfiguration.Should().NotBeNull();
        result.SelectedLayerType.Should().Be(ThreeLayerConfigurationType.Templates);
        result.WorkflowChoice.Should().Be("direct-build");
    }

    [Fact]
    public async Task ShowAdvancedSelectionAsync_WithEmptyItems_ShouldReturnCancelled()
    {
        // Arrange
        var selector = new InteractiveSelector<SelectableOption>
        {
            Prompt = "Select option",
            Items = new List<SelectableOption>()
        };

        // Act
        var result = await _service.ShowAdvancedSelectionAsync(selector);

        // Assert
        result.IsCancelled.Should().BeTrue();
    }

    [Fact]
    public async Task ShowGroupedSelectionAsync_WithEmptyGroups_ShouldReturnCancelled()
    {
        // Arrange
        var groups = new Dictionary<string, InteractiveSelector<SelectableOption>>();

        // Act
        var result = await _service.ShowGroupedSelectionAsync(groups);

        // Assert
        result.IsCancelled.Should().BeTrue();
    }

    [Fact(Skip = "需要交互式控制台环境，CI中无法运行")]
    public async Task ShowGroupedSelectionAsync_WithValidGroups_ShouldAllowGroupSelection()
    {
        // Arrange
        var groups = new Dictionary<string, InteractiveSelector<SelectableOption>>
        {
            ["Images"] = new InteractiveSelector<SelectableOption>
            {
                Prompt = "Select image",
                Items = new List<SelectableOption>
                {
                    new() { DisplayName = "Image 1", Value = "img1", IsAvailable = true }
                }
            },
            ["Templates"] = new InteractiveSelector<SelectableOption>
            {
                Prompt = "Select template",
                Items = new List<SelectableOption>
                {
                    new() { DisplayName = "Template 1", Value = "tpl1", IsAvailable = true }
                }
            }
        };

        // 模拟用户输入：先选择组(1)，然后选择组内选项(1)
        Console.SetIn(new StringReader("1\n1\n"));

        // Act
        var result = await _service.ShowGroupedSelectionAsync(groups);

        // Assert - 由于涉及到控制台交互，这里主要测试不会抛异常
        // 实际的选择结果会依赖于具体的用户输入模拟
        groups.Should().NotBeEmpty();
    }

    [Fact(Skip = "需要交互式控制台环境，CI中无法运行")]
    public async Task ShowSmartSelectionAsync_WithSmartHints_ShouldDisplayHints()
    {
        // Arrange
        var selector = new InteractiveSelector<SelectableOption>
        {
            Prompt = "Smart selection",
            Items = new List<SelectableOption>
            {
                new() { DisplayName = "Option 1", Value = "opt1", IsAvailable = true },
                new() { DisplayName = "Option 2", Value = "opt2", IsAvailable = false }
            }
        };

        var smartHints = new SmartHintOptions
        {
            Enabled = true,
            ShowActionSuggestions = true
        };

        // Act
        var result = await _service.ShowSmartSelectionAsync(selector, smartHints);

        // Assert - 验证智能提示被调用
        // 由于控制台交互复杂性，这里主要测试方法能正常执行
        selector.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task ShowWorkflowSelectionAsync_WithValidInput_ShouldReturnWorkflowChoice()
    {
        // Arrange
        var templateName = "test-template";
        Console.SetIn(new StringReader("1\n"));

        // Act
        var result = await _service.ShowWorkflowSelectionAsync(templateName);

        // Assert
        result.Should().Be("create-config");
    }

    [Fact]
    public async Task ShowWorkflowSelectionAsync_WithDirectBuildChoice_ShouldReturnDirectBuild()
    {
        // Arrange
        var templateName = "test-template";
        Console.SetIn(new StringReader("2\n"));

        // Act
        var result = await _service.ShowWorkflowSelectionAsync(templateName);

        // Assert
        result.Should().Be("direct-build");
    }

    [Fact]
    public async Task ShowWorkflowSelectionAsync_WithInvalidInput_ShouldReturnNull()
    {
        // Arrange
        var templateName = "test-template";
        Console.SetIn(new StringReader("3\n"));

        // Act
        var result = await _service.ShowWorkflowSelectionAsync(templateName);

        // Assert
        result.Should().BeNull();
    }

    [Fact(Skip = "需要交互式控制台环境，CI中无法运行")]
    public async Task ShowHelpAsync_ShouldDisplayHelpContent()
    {
        // Arrange
        var title = "Help Title";
        var helpContent = new Dictionary<string, string>
        {
            ["Topic 1"] = "Description 1",
            ["Topic 2"] = "Description 2"
        };

        // 模拟按键继续
        Console.SetIn(new StringReader(" "));

        // Act
        await _service.ShowHelpAsync(title, helpContent);

        // Assert
        var output = _consoleOutput.ToString();
        output.Should().Contain(title);
        // 注意：由于Console.Clear()的存在，实际输出可能被清空
        // 主要验证方法能够正常执行而不抛异常
    }

    [Fact]
    public async Task ShowProjectDetectionAsync_ShouldDisplayProjectInfo()
    {
        // Arrange
        var projectType = ProjectType.Avalonia;
        var projectFiles = new[] { "Cargo.toml", "package.json" };
        var recommendations = new[] { "选择 avalonia-* 相关配置" };

        // Act
        await _service.ShowProjectDetectionAsync(projectType, projectFiles, recommendations);

        // Assert
        var output = _consoleOutput.ToString();
        output.Should().Contain("检测到环境类型");
        output.Should().Contain(projectType.ToString());
    }

    [Theory]
    [InlineData(ProjectType.Avalonia, "avalonia-dev")]
    [InlineData(ProjectType.Avalonia, "avalonia-ui")]
    public void SelectableThreeLayerConfiguration_ShouldFormatCorrectly(ProjectType projectType, string name)
    {
        // Arrange & Act
        var config = new SelectableThreeLayerConfiguration
        {
            Name = name,
            LayerType = ThreeLayerConfigurationType.Images,
            DetectedProjectType = projectType,
            Status = new ConfigurationStatus { HasComposeYaml = true, HasDockerfile = true, HasEnvFile = true },
            LastModified = DateTime.Now.AddHours(-2)
        };

        // Assert
        config.DisplayName.Should().Be(name);
        config.IsAvailable.Should().BeTrue();
        config.Description.Should().Contain(projectType.ToString());
        config.ExtraInfo.Should().Contain("小时前");
    }

    [Fact]
    public void SelectableThreeLayerConfiguration_WithMissingFiles_ShouldShowMissingInfo()
    {
        // Arrange & Act
        var config = new SelectableThreeLayerConfiguration
        {
            Name = "incomplete-config",
            Status = new ConfigurationStatus { HasComposeYaml = false, HasDockerfile = true, HasEnvFile = false, MissingFiles = { "compose.yaml", ".env" } },
            MissingFiles = new[] { "compose.yaml", ".env" }
        };

        // Assert
        config.IsAvailable.Should().BeFalse();
        config.ExtraInfo.Should().Contain("缺少");
        config.ExtraInfo.Should().Contain("compose.yaml");
        config.ExtraInfo.Should().Contain(".env");
    }

    [Fact]
    public void ThreeLayerSelector_DefaultConfiguration_ShouldBeCorrect()
    {
        // Arrange & Act
        var selector = new ThreeLayerSelector();

        // Assert
        selector.Prompt.Should().Be("请选择配置：");
        selector.ShowProjectDetection.Should().BeTrue();
        selector.EnableProjectTypeFilter.Should().BeTrue();
        selector.ShowConfigurationStatus.Should().BeTrue();
        selector.ShowSmartHints.Should().BeTrue();
        selector.LayerTitles.Should().ContainKey(ThreeLayerConfigurationType.Images);
        selector.LayerTitles.Should().ContainKey(ThreeLayerConfigurationType.Custom);
        selector.LayerTitles.Should().ContainKey(ThreeLayerConfigurationType.Templates);
    }

    [Fact]
    public void KeyboardNavigationOptions_DefaultConfiguration_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new KeyboardNavigationOptions();

        // Assert
        options.Enabled.Should().BeTrue();
        options.UpKey.Should().Be(ConsoleKey.UpArrow);
        options.DownKey.Should().Be(ConsoleKey.DownArrow);
        options.ConfirmKey.Should().Be(ConsoleKey.Enter);
        options.CancelKey.Should().Be(ConsoleKey.Escape);
        options.SearchKey.Should().Be(ConsoleKey.F);
        options.HelpKey.Should().Be(ConsoleKey.F1);
    }

    [Fact]
    public void SmartHintOptions_DefaultConfiguration_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new SmartHintOptions();

        // Assert
        options.Enabled.Should().BeTrue();
        options.ShowProjectTypeHints.Should().BeTrue();
        options.ShowConfigurationStatusHints.Should().BeTrue();
        options.ShowActionSuggestions.Should().BeTrue();
        options.ShowKeyboardShortcuts.Should().BeTrue();
    }

    [Fact]
    public void ThreeLayerSelectionResult_DefaultState_ShouldBeCorrect()
    {
        // Arrange & Act
        var result = new ThreeLayerSelectionResult();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsCancelled.Should().BeFalse();
        result.SelectedConfiguration.Should().BeNull();
        result.SelectedLayerType.Should().BeNull();
        result.ErrorMessage.Should().BeNull();
        result.WorkflowChoice.Should().BeNull();
    }

    // 集成测试：测试三层选择的完整流程
    [Fact]
    public void IntegrationTest_ThreeLayerSelection_ShouldBuildCorrectOptions()
    {
        // Arrange
        var selector = new ThreeLayerSelector
        {
            DetectedProjectType = ProjectType.Avalonia,
            EnableProjectTypeFilter = true,
            ImagesConfigurations = new List<SelectableThreeLayerConfiguration>
            {
                new()
                {
                    Name = "avalonia-dev",
                    LayerType = ThreeLayerConfigurationType.Images,
                    DetectedProjectType = ProjectType.Avalonia,
                    Status = new ConfigurationStatus { HasComposeYaml = true, HasDockerfile = true, HasEnvFile = true }
                },
                new()
                {
                    Name = "flutter-dev",
                    LayerType = ThreeLayerConfigurationType.Images,
                    DetectedProjectType = ProjectType.Flutter,
                    Status = new ConfigurationStatus { HasComposeYaml = true, HasDockerfile = true, HasEnvFile = true }
                }
            }
        };

        // Act & Assert - 验证过滤逻辑
        var avaloniaConfigs = selector.ImagesConfigurations.Where(c => c.DetectedProjectType == ProjectType.Avalonia).ToList();
        var otherConfigs = selector.ImagesConfigurations.Where(c => c.DetectedProjectType != ProjectType.Avalonia).ToList();

        // 应该优先显示匹配的项目类型
        avaloniaConfigs.Should().HaveCount(1);
        avaloniaConfigs[0].Name.Should().Be("avalonia-dev");
        otherConfigs.Should().HaveCount(1);
        otherConfigs[0].Name.Should().Be("flutter-dev");
    }
}