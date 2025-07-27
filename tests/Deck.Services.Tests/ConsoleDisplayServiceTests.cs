using Deck.Core.Interfaces;
using Deck.Core.Models;
using Deck.Services;
using FluentAssertions;
using System.ComponentModel;

namespace Deck.Services.Tests;

/// <summary>
/// ConsoleDisplayService 单元测试
/// 测试控制台显示服务的各项功能
/// </summary>
[Collection("ConsoleDisplayTests")]
[CollectionDefinition("ConsoleDisplayTests", DisableParallelization = true)]
public class ConsoleDisplayServiceTests : IDisposable
{
    private readonly IConsoleDisplay _consoleDisplay;
    private readonly StringWriter _stringWriter;
    private readonly TextWriter _originalOut;

    public ConsoleDisplayServiceTests()
    {
        // 不要在构造函数中设置全局 Console.Out，因为这会影响并行测试
        _consoleDisplay = new ConsoleDisplayService();
        _stringWriter = new StringWriter();
        _originalOut = Console.Out;
    }

    public void Dispose()
    {
        // 确保恢复原始输出
        if (Console.Out != _originalOut)
        {
            Console.SetOut(_originalOut);
        }
        _stringWriter?.Dispose();
    }

    private void CaptureConsoleOutput(Action action)
    {
        // 临时重定向控制台输出并执行操作
        var originalOut = Console.Out;
        try
        {
            Console.SetOut(_stringWriter);
            action();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    [Description("测试基础文本输出功能")]
    public void WriteLine_ShouldOutputText()
    {
        // Arrange
        var testMessage = "Hello World";

        // Act
        CaptureConsoleOutput(() => 
        {
            _consoleDisplay.WriteLine(testMessage);
        });

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain(testMessage);
    }

    [Fact]
    [Description("测试彩色文本输出功能")]
    public void WriteLine_WithColor_ShouldOutputColoredText()
    {
        // Arrange
        var testMessage = "Colored Message";

        // Act
        CaptureConsoleOutput(() => 
        {
            _consoleDisplay.WriteLine(testMessage, ConsoleColor.Red);
        });

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain(testMessage);
        output.Should().Contain("\u001b[31m"); // Red color code
        output.Should().Contain("\u001b[0m");  // Reset code
    }

    [Fact]
    [Description("测试成功消息显示")]
    public void ShowSuccess_ShouldDisplaySuccessMessage()
    {
        // Arrange
        var message = "Operation successful";

        // Act
        CaptureConsoleOutput(() => 
        {
            _consoleDisplay.ShowSuccess(message);
        });

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("✓");
        output.Should().Contain(message);
        output.Should().Contain("\u001b[32m"); // Green color
    }

    [Fact]
    [Description("测试错误消息显示")]
    public void ShowError_ShouldDisplayErrorMessage()
    {
        // Arrange
        var message = "Error occurred";

        // Act
        CaptureConsoleOutput(() => 
        {
            _consoleDisplay.ShowError(message);
        });

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("✗");
        output.Should().Contain(message);
        output.Should().Contain("\u001b[31m"); // Red color
    }

    [Fact]
    [Description("测试警告消息显示")]
    public void ShowWarning_ShouldDisplayWarningMessage()
    {
        // Arrange
        var message = "Warning message";

        // Act
        CaptureConsoleOutput(() => 
        {
            _consoleDisplay.ShowWarning(message);
        });

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("⚠");
        output.Should().Contain(message);
        output.Should().Contain("\u001b[33m"); // Yellow color
    }

    [Fact]
    [Description("测试信息消息显示")]
    public void ShowInfo_ShouldDisplayInfoMessage()
    {
        // Arrange
        var message = "Information";

        // Act
        CaptureConsoleOutput(() => 
        {
            _consoleDisplay.ShowInfo(message);
        });

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("ℹ");
        output.Should().Contain(message);
        output.Should().Contain("\u001b[36m"); // Cyan color
    }

    [Fact]
    [Description("测试标题显示")]
    public void ShowTitle_ShouldDisplayFormattedTitle()
    {
        // Arrange
        var title = "Main Title";

        // Act
        CaptureConsoleOutput(() => 
        {
            _consoleDisplay.ShowTitle(title);
        });

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain(title);
        output.Should().Contain("=");
        output.Should().Contain("\u001b[33m"); // Yellow color for title
    }

    [Fact]
    [Description("测试小标题显示")]
    public void ShowSubtitle_ShouldDisplayFormattedSubtitle()
    {
        // Arrange
        var subtitle = "Sub Title";

        // Act
        CaptureConsoleOutput(() => 
        {
            _consoleDisplay.ShowSubtitle(subtitle);
        });

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain(subtitle);
        output.Should().Contain("──");
        output.Should().Contain("\u001b[36m"); // Cyan color
    }

    [Fact]
    [Description("测试分隔符显示")]
    public void ShowSeparator_ShouldDisplaySeparator()
    {
        // Arrange
        var length = 10;
        var character = '-';

        // Act
        CaptureConsoleOutput(() => 
        {
            _consoleDisplay.ShowSeparator(length, character);
        });

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain(new string(character, length));
    }

    [Fact]
    [Description("测试列表显示")]
    public void ShowList_ShouldDisplayListItems()
    {
        // Arrange
        var items = new[] { "Item 1", "Item 2", "Item 3" };

        // Act
        CaptureConsoleOutput(() => 
        {
            _consoleDisplay.ShowList(items);
        });

        // Assert
        var output = _stringWriter.ToString();
        foreach (var item in items)
        {
            output.Should().Contain(item);
        }
        output.Should().Contain("1.");
        output.Should().Contain("2.");
        output.Should().Contain("3.");
    }

    [Fact]
    [Description("测试表格显示")]
    public void ShowTable_ShouldDisplayFormattedTable()
    {
        // Arrange
        var headers = new[] { "Name", "Age", "City" };
        var rows = new[]
        {
            new[] { "Alice", "30", "New York" },
            new[] { "Bob", "25", "Los Angeles" }
        };

        // Act
        CaptureConsoleOutput(() => 
        {
            _consoleDisplay.ShowTable(headers, rows);
        });

        // Assert
        var output = _stringWriter.ToString();
        
        // Check headers
        foreach (var header in headers)
        {
            output.Should().Contain(header);
        }
        
        // Check data
        output.Should().Contain("Alice");
        output.Should().Contain("30");
        output.Should().Contain("New York");
        output.Should().Contain("Bob");
        output.Should().Contain("25");
        output.Should().Contain("Los Angeles");
        
        // Check formatting
        output.Should().Contain("#"); // Index column
        output.Should().Contain("│"); // Column separator
        output.Should().Contain("-"); // Row separator
    }

    [Fact]
    [Description("测试进度显示")]
    public void ShowProgress_ShouldDisplayProgressBar()
    {
        // Arrange
        var current = 50;
        var total = 100;
        var message = "Processing...";

        // Act
        CaptureConsoleOutput(() => 
        {
            _consoleDisplay.ShowProgress(current, total, message);
        });

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("50.0%");
        output.Should().Contain("(50/100)");
        output.Should().Contain(message);
        output.Should().Contain("█"); // Progress bar filled character
        output.Should().Contain("░"); // Progress bar empty character
    }

    [Fact]
    [Description("测试步骤显示")]
    public void ShowStep_ShouldDisplayStepInfo()
    {
        // Arrange
        var stepNumber = 2;
        var totalSteps = 5;
        var description = "Installing packages";

        // Act
        CaptureConsoleOutput(() => 
        {
            _consoleDisplay.ShowStep(stepNumber, totalSteps, description);
        });

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("[2/5]");
        output.Should().Contain(description);
        output.Should().Contain("\u001b[36m"); // Cyan color
    }

    [Fact]
    [Description("测试图标消息显示")]
    public void ShowIconMessage_ShouldDisplayIconWithMessage()
    {
        // Arrange
        var icon = "🚀";
        var message = "Launch successful";

        // Act
        CaptureConsoleOutput(() => 
        {
            _consoleDisplay.ShowIconMessage(icon, message, ConsoleColor.Green);
        });

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain(icon);
        output.Should().Contain(message);
        output.Should().Contain("\u001b[32m"); // Green color
    }

    [Fact]
    [Description("测试边框显示")]
    public void ShowBox_ShouldDisplayBoxWithContent()
    {
        // Arrange
        var content = "This is a test message";
        var title = "Test Box";

        // Act
        CaptureConsoleOutput(() => 
        {
            _consoleDisplay.ShowBox(content, title);
        });

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain(content);
        output.Should().Contain(title);
        output.Should().Contain("┌"); // Top-left corner
        output.Should().Contain("┐"); // Top-right corner
        output.Should().Contain("└"); // Bottom-left corner
        output.Should().Contain("┘"); // Bottom-right corner
        output.Should().Contain("─"); // Horizontal line
        output.Should().Contain("│"); // Vertical line
    }

    [Fact]
    [Description("测试多行边框显示")]
    public void ShowBox_WithMultipleLines_ShouldDisplayMultiLineBox()
    {
        // Arrange
        var lines = new[] { "Line 1", "Line 2", "Line 3" };

        // Act
        CaptureConsoleOutput(() => 
        {
            _consoleDisplay.ShowBox(lines);
        });

        // Assert
        var output = _stringWriter.ToString();
        foreach (var line in lines)
        {
            output.Should().Contain(line);
        }
        
        // Should contain box drawing characters
        output.Should().Contain("┌");
        output.Should().Contain("└");
        output.Should().Contain("│");
    }

    /// <summary>
    /// 测试用的可选择项目实现
    /// </summary>
    private class TestSelectableItem : ISelectableItem
    {
        public string DisplayName { get; set; } = "";
        public string? Description { get; set; }
        public bool IsAvailable { get; set; } = true;
        public string Value { get; set; } = "";
        public string? ExtraInfo { get; set; }
    }

    [Fact]
    [Description("测试可选择列表显示")]
    public void ShowSelectableList_ShouldDisplaySelectableItems()
    {
        // Arrange
        var items = new[]
        {
            new TestSelectableItem { DisplayName = "Option 1", Description = "First option", IsAvailable = true },
            new TestSelectableItem { DisplayName = "Option 2", Description = "Second option", IsAvailable = false },
            new TestSelectableItem { DisplayName = "Option 3", IsAvailable = true }
        };
        var title = "Available Options";

        // Act
        CaptureConsoleOutput(() => 
        {
            _consoleDisplay.ShowSelectableList(items, title);
        });

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain(title);
        output.Should().Contain("Option 1");
        output.Should().Contain("First option");
        output.Should().Contain("Option 2");
        output.Should().Contain("Second option");
        output.Should().Contain("Option 3");
        output.Should().Contain("不可用");
        output.Should().Contain("1.");
        output.Should().Contain("2.");
        output.Should().Contain("3.");
    }

#if DEBUG
    [Fact]
    [Description("测试调试消息显示（仅在DEBUG模式下）")]
    public void ShowDebug_InDebugMode_ShouldDisplayDebugMessage()
    {
        // Arrange
        var message = "Debug information";

        // Act
        CaptureConsoleOutput(() => 
        {
            _consoleDisplay.ShowDebug(message);
        });

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("🐛");
        output.Should().Contain("[DEBUG]");
        output.Should().Contain(message);
    }
#endif

    [Fact]
    [Description("测试服务创建和基础功能")]
    public void ConsoleDisplayService_ShouldBeCreatable()
    {
        // Act & Assert
        var service = new ConsoleDisplayService();
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<IConsoleDisplay>();
    }

    [Fact]
    [Description("测试空列表显示")]
    public void ShowList_WithEmptyList_ShouldNotThrow()
    {
        // Arrange
        var emptyList = Array.Empty<string>();

        // Act & Assert
        var act = () => _consoleDisplay.ShowList(emptyList);
        act.Should().NotThrow();
    }

    [Fact]
    [Description("测试空表格显示")]
    public void ShowTable_WithEmptyData_ShouldShowWarning()
    {
        // Arrange
        var headers = Array.Empty<string>();
        var rows = Array.Empty<string[]>();

        // Act
        CaptureConsoleOutput(() => 
        {
            _consoleDisplay.ShowTable(headers, rows);
        });

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("表格数据为空");
    }
}