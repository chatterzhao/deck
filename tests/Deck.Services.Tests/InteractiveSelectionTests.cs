using Deck.Core.Interfaces;
using Deck.Core.Models;
using FluentAssertions;

namespace Deck.Services.Tests;

public class InteractiveSelectionTests
{
    [Fact]
    public void SelectableOption_ShouldImplementISelectableItem()
    {
        // Arrange
        var option = new SelectableOption
        {
            DisplayName = "Test Option",
            Description = "This is a test",
            IsAvailable = true,
            Value = "test_value",
            ExtraInfo = "Extra info"
        };

        // Act & Assert
        option.Should().BeAssignableTo<ISelectableItem>();
        option.DisplayName.Should().Be("Test Option");
        option.Description.Should().Be("This is a test");
        option.IsAvailable.Should().BeTrue();
        option.Value.Should().Be("test_value");
        option.ExtraInfo.Should().Be("Extra info");
    }

    [Fact]
    public void ConfigurationOption_ShouldImplementISelectableItem()
    {
        // Arrange
        var config = new ConfigurationOption
        {
            Name = "my-app",
            Type = ConfigurationType.Images,
            Path = "/path/to/config",
            ProjectType = ProjectType.DotNet,
            IsAvailable = true,
            Description = "My application",
            LastModified = new DateTime(2024, 1, 1, 12, 0, 0)
        };

        // Act & Assert
        config.Should().BeAssignableTo<ISelectableItem>();
        config.DisplayName.Should().Be("my-app");
        config.Value.Should().Be("/path/to/config");
        config.ExtraInfo.Should().Be("Images | 2024-01-01 12:00");
        config.Description.Should().Be("My application");
        config.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void SelectableImage_ShouldFormatCorrectly()
    {
        // Arrange
        var imageInfo = new ImageInfo
        {
            Id = "sha256:abcdef1234567890",
            Name = "nginx",
            Tag = "latest",
            Created = new DateTime(2024, 1, 1),
            Size = 1024 * 1024 * 100, // 100MB
            Exists = true
        };

        var selectableImage = new SelectableImage(imageInfo);

        // Act & Assert
        selectableImage.DisplayName.Should().Be("nginx:latest");
        selectableImage.Description.Should().Be("ID: sha256:abcde");
        selectableImage.IsAvailable.Should().BeTrue();
        selectableImage.Value.Should().Be("sha256:abcdef1234567890");
        selectableImage.ExtraInfo.Should().Be("100 MB | 2024-01-01");
        selectableImage.ImageInfo.Should().Be(imageInfo);
    }

    [Fact]
    public void SelectableContainer_ShouldFormatCorrectly()
    {
        // Arrange
        var container = new SelectableContainer
        {
            ContainerId = "abcdef123456",
            ContainerName = "my-app",
            Status = ContainerStatus.Running,
            ImageName = "nginx:latest",
            Created = new DateTime(2024, 1, 1, 12, 0, 0),
            Ports = new Dictionary<string, string>
            {
                ["80"] = "8080",
                ["443"] = "8443"
            }
        };

        // Act & Assert
        container.DisplayName.Should().Be("my-app");
        container.Description.Should().Be("nginx:latest | Running");
        container.IsAvailable.Should().BeTrue();
        container.Value.Should().Be("abcdef123456");
        container.ExtraInfo.Should().Be("2024-01-01 12:00 | 80→8080, 443→8443");
    }

    [Fact]
    public void SelectableContainer_WithoutName_ShouldUseTruncatedId()
    {
        // Arrange
        var container = new SelectableContainer
        {
            ContainerId = "abcdef123456789",
            ContainerName = "",
            Status = ContainerStatus.Stopped
        };

        // Act & Assert
        container.DisplayName.Should().Be("abcdef123456");
    }

    [Fact]
    public void SelectableContainer_WithoutPorts_ShouldShowNoPortsMessage()
    {
        // Arrange
        var container = new SelectableContainer
        {
            ContainerId = "abcdef123456",
            Status = ContainerStatus.Running
        };

        // Act & Assert
        container.ExtraInfo.Should().Contain("No ports");
    }

    [Fact]
    public void SelectableProjectType_ShouldProvideCorrectDescriptions()
    {
        // Arrange
        var projectTypes = new[]
        {
            ProjectType.DotNet,
            ProjectType.Python,
            ProjectType.Node,
            ProjectType.Flutter,
            ProjectType.Tauri,
            ProjectType.Avalonia,
            ProjectType.Unknown
        };

        // Act & Assert
        foreach (var projectType in projectTypes)
        {
            var selectable = new SelectableProjectType
            {
                ProjectType = projectType,
                SupportedExtensions = [".cs", ".py", ".js"],
                IsAvailable = true
            };

            selectable.DisplayName.Should().Be(projectType.ToString());
            selectable.Value.Should().Be(projectType.ToString());
            selectable.Description.Should().NotBeNullOrEmpty();
            selectable.ExtraInfo.Should().Contain(".cs, .py, .js");
        }
    }

    [Fact]
    public void SelectableCommand_ShouldFormatCorrectly()
    {
        // Arrange
        var command = new SelectableCommand
        {
            Command = "build",
            Description = "Build the project",
            Aliases = ["b", "compile"],
            RequiresConfirmation = false,
            Category = "Build",
            IsAvailable = true
        };

        // Act & Assert
        command.DisplayName.Should().Be("build");
        command.Description.Should().Be("Build the project");
        command.Value.Should().Be("build");
        command.ExtraInfo.Should().Be("Build | Aliases: b, compile");
        command.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void SelectableCommand_WithoutAliases_ShouldFormatCorrectly()
    {
        // Arrange
        var command = new SelectableCommand
        {
            Command = "clean",
            Category = "Cleanup",
            Aliases = []
        };

        // Act & Assert
        command.ExtraInfo.Should().Be("Cleanup");
    }

    [Fact]
    public void InteractiveSelector_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var selector = new InteractiveSelector<SelectableOption>();

        // Assert
        selector.Prompt.Should().Be("");
        selector.Items.Should().BeEmpty();
        selector.AllowMultiple.Should().BeFalse();
        selector.Required.Should().BeTrue();
        selector.DefaultIndex.Should().Be(0);
        selector.PageSize.Should().Be(10);
        selector.ShowIndex.Should().BeTrue();
        selector.ShowDescription.Should().BeTrue();
        selector.EnableSearch.Should().BeTrue();
    }

    [Fact]
    public void SelectionResult_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var result = new SelectionResult<SelectableOption>();

        // Assert
        result.IsCancelled.Should().BeFalse();
        result.SelectedItem.Should().BeNull();
        result.SelectedItems.Should().BeEmpty();
        result.SelectedIndex.Should().Be(-1);
        result.SelectedIndices.Should().BeEmpty();
    }

    [Fact]
    public void SelectionStyle_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var style = new SelectionStyle();

        // Assert
        style.DisplayMode.Should().Be(SelectionDisplayMode.List);
        style.HighlightColor.Should().Be(ConsoleColor.Cyan);
        style.SelectedColor.Should().Be(ConsoleColor.Green);
        style.DisabledColor.Should().Be(ConsoleColor.DarkGray);
        style.BorderStyle.Should().Be(BorderStyle.Simple);
        style.ShowBorder.Should().BeTrue();
        style.IndentSpaces.Should().Be(2);
    }

    [Fact]
    public void ProgressInfo_CalculatePercentage_ShouldWork()
    {
        // Arrange & Act
        var progress1 = new ProgressInfo { Current = 50, Total = 100 };
        var progress2 = new ProgressInfo { Current = 25, Total = 200 };
        var progress3 = new ProgressInfo { Current = 10, Total = 0 }; // Edge case

        // Assert
        progress1.Percentage.Should().Be(50.0);
        progress2.Percentage.Should().Be(12.5);
        progress3.Percentage.Should().Be(0.0);
    }
}