using Deck.Core.Interfaces;
using Deck.Core.Models;
using Deck.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Net.Sockets;

namespace Deck.Services.Tests;

/// <summary>
/// 端口冲突服务测试
/// </summary>
public class PortConflictServiceTests : IDisposable
{
    private readonly PortConflictService _portConflictService;
    private readonly List<TcpListener> _testListeners = new();

    public PortConflictServiceTests()
    {
        _portConflictService = new PortConflictService(NullLogger<PortConflictService>.Instance);
    }

    [Fact]
    public async Task CheckPortAsync_WithAvailablePort_ShouldReturnAvailable()
    {
        // Arrange - 查找一个可用端口
        var availablePort = GetAvailablePort();

        // Act
        var result = await _portConflictService.CheckPortAsync(availablePort, DeckProtocolType.TCP);

        // Assert
        result.Should().NotBeNull();
        result.Port.Should().Be(availablePort);
        result.Protocol.Should().Be(DeckProtocolType.TCP);
        result.IsAvailable.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.ResponseTimeMs.Should().BeGreaterOrEqualTo(0);
        result.CheckTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CheckPortAsync_WithOccupiedPort_ShouldReturnOccupied()
    {
        // Arrange - 占用一个端口
        var port = GetAvailablePort();
        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        _testListeners.Add(listener);

        // Act
        var result = await _portConflictService.CheckPortAsync(port, DeckProtocolType.TCP);

        // Assert
        result.Should().NotBeNull();
        result.Port.Should().Be(port);
        result.Protocol.Should().Be(DeckProtocolType.TCP);
        result.IsAvailable.Should().BeFalse();
        result.ResponseTimeMs.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task CheckPortsAsync_WithMultiplePorts_ShouldReturnAllResults()
    {
        // Arrange
        var availablePort1 = GetAvailablePort();
        var availablePort2 = GetAvailablePort(availablePort1 + 1);
        var occupiedPort = GetAvailablePort(availablePort2 + 1);
        
        // 占用一个端口
        var listener = new TcpListener(IPAddress.Any, occupiedPort);
        listener.Start();
        _testListeners.Add(listener);

        var ports = new[] { availablePort1, availablePort2, occupiedPort };

        // Act
        var results = await _portConflictService.CheckPortsAsync(ports, DeckProtocolType.TCP);

        // Assert
        results.Should().HaveCount(3);
        results[0].Port.Should().Be(availablePort1);
        results[0].IsAvailable.Should().BeTrue();
        results[1].Port.Should().Be(availablePort2);
        results[1].IsAvailable.Should().BeTrue();
        results[2].Port.Should().Be(occupiedPort);
        results[2].IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task DetectPortConflictAsync_WithAvailablePort_ShouldReturnNoConflict()
    {
        // Arrange
        var availablePort = GetAvailablePort();

        // Act
        var result = await _portConflictService.DetectPortConflictAsync(availablePort, DeckProtocolType.TCP);

        // Assert
        result.Should().NotBeNull();
        result.Port.Should().Be(availablePort);
        result.Protocol.Should().Be(DeckProtocolType.TCP);
        result.HasConflict.Should().BeFalse();
        result.DetectionTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task DetectPortConflictAsync_WithOccupiedPort_ShouldReturnConflict()
    {
        // Arrange - 占用一个端口
        var port = GetAvailablePort();
        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        _testListeners.Add(listener);

        // Act
        var result = await _portConflictService.DetectPortConflictAsync(port, DeckProtocolType.TCP);

        // Assert
        result.Should().NotBeNull();
        result.Port.Should().Be(port);
        result.Protocol.Should().Be(DeckProtocolType.TCP);
        result.HasConflict.Should().BeTrue();
        // 注意：在测试环境中可能无法获取进程信息，所以不强制要求
    }

    [Fact]
    public async Task FindAvailablePortAsync_WithPreferredPort_ShouldReturnPreferredIfAvailable()
    {
        // Arrange
        var preferredPort = GetAvailablePort();

        // Act
        var result = await _portConflictService.FindAvailablePortAsync(
            preferredPort, preferredPort, preferredPort + 10, DeckProtocolType.TCP);

        // Assert
        result.Should().Be(preferredPort);
    }

    [Fact]
    public async Task FindAvailablePortAsync_WithOccupiedPreferredPort_ShouldReturnAlternative()
    {
        // Arrange - 占用首选端口
        var preferredPort = GetAvailablePort();
        var listener = new TcpListener(IPAddress.Any, preferredPort);
        listener.Start();
        _testListeners.Add(listener);

        // Act
        var result = await _portConflictService.FindAvailablePortAsync(
            preferredPort, preferredPort, preferredPort + 10, DeckProtocolType.TCP);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBe(preferredPort);
        result.Should().BeGreaterThan(preferredPort);
        result.Should().BeLessOrEqualTo(preferredPort + 10);
    }

    [Fact]
    public async Task AllocateProjectPortsAsync_ForDotNetProject_ShouldAllocatePorts()
    {
        // Act
        var result = await _portConflictService.AllocateProjectPortsAsync(
            ProjectType.DotNet, 
            ProjectPortType.Api, 
            ProjectPortType.Debug);

        // Assert
        result.Should().NotBeNull();
        result.ProjectType.Should().Be(ProjectType.DotNet);
        result.AllocatedPorts.Should().ContainKey(ProjectPortType.Api);
        result.AllocatedPorts.Should().ContainKey(ProjectPortType.Debug);
        result.AllocationSummary.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AllocateProjectPortsAsync_ForUnsupportedProject_ShouldReturnFailures()
    {
        // Act
        var result = await _portConflictService.AllocateProjectPortsAsync(
            ProjectType.Unknown, 
            ProjectPortType.Api);

        // Assert
        result.Should().NotBeNull();
        result.ProjectType.Should().Be(ProjectType.Unknown);
        result.FailedAllocations.Should().Contain(ProjectPortType.Api);
        result.AllocatedPorts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetResolutionSuggestionsAsync_WithConflict_ShouldReturnSuggestions()
    {
        // Arrange
        var conflictInfo = new PortConflictInfo
        {
            Port = 8080,
            Protocol = DeckProtocolType.TCP,
            HasConflict = true,
            Severity = ConflictSeverity.Medium
        };

        // Act
        var suggestions = await _portConflictService.GetResolutionSuggestionsAsync(conflictInfo);

        // Assert
        suggestions.Should().NotBeEmpty();
        suggestions.Should().Contain(s => s.Type == ResolutionType.UseAlternativePort);
        suggestions.Should().Contain(s => s.Type == ResolutionType.ModifyConfiguration);
    }

    [Fact]
    public async Task GetResolutionSuggestionsAsync_WithNoConflict_ShouldReturnEmpty()
    {
        // Arrange
        var conflictInfo = new PortConflictInfo
        {
            Port = 8080,
            Protocol = DeckProtocolType.TCP,
            HasConflict = false
        };

        // Act
        var suggestions = await _portConflictService.GetResolutionSuggestionsAsync(conflictInfo);

        // Assert
        suggestions.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidatePortAsync_WithValidPort_ShouldReturnValid()
    {
        // Act
        var result = await _portConflictService.ValidatePortAsync(8080, checkPrivileged: false);

        // Assert
        result.Should().NotBeNull();
        result.Port.Should().Be(8080);
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.RequiresPrivilege.Should().BeFalse();
    }

    [Fact]
    public async Task ValidatePortAsync_WithPrivilegedPort_ShouldRequirePrivilege()
    {
        // Act
        var result = await _portConflictService.ValidatePortAsync(80, checkPrivileged: true);

        // Assert
        result.Should().NotBeNull();
        result.Port.Should().Be(80);
        result.RequiresPrivilege.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("特权端口"));
        result.SuggestedAlternatives.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ValidatePortAsync_WithInvalidPort_ShouldReturnInvalid()
    {
        // Act
        var result = await _portConflictService.ValidatePortAsync(70000);

        // Assert
        result.Should().NotBeNull();
        result.Port.Should().Be(70000);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("超出有效范围"));
    }

    [Fact]
    public async Task GetSystemPortUsageAsync_ShouldReturnUsageInfo()
    {
        // Act
        var usage = await _portConflictService.GetSystemPortUsageAsync();

        // Assert
        usage.Should().NotBeNull();
        usage.ScanTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        usage.Statistics.Should().NotBeNull();
        // 注意：实际的端口数量依赖于系统状态，所以不强制验证具体数值
    }

    [Fact]
    public async Task AllocateProjectPortsAsync_ShouldUseExpectedDefaultPorts_ForAvalonia()
    {
        // Act - 只测试 Avalonia 模板的端口分配
        var result = await _portConflictService.AllocateProjectPortsAsync(ProjectType.Avalonia, ProjectPortType.DevServer);

        // Assert
        result.Should().NotBeNull();
        if (result.AllocatedPorts.TryGetValue(ProjectPortType.DevServer, out var allocatedPort))
        {
            // Avalonia 的默认 DevServer 端口是 5000，端口可能因为冲突而调整
            allocatedPort.Should().BeInRange(5000, 5100);
        }
        else
        {
            // 如果分配失败，应该在失败列表中
            result.FailedAllocations.Should().Contain(ProjectPortType.DevServer);
        }
    }

    [Fact]
    public void PortConflictInfo_ShouldInitializeCorrectly()
    {
        // Act
        var info = new PortConflictInfo
        {
            Port = 8080,
            Protocol = DeckProtocolType.TCP,
            HasConflict = true,
            Severity = ConflictSeverity.High
        };

        // Assert
        info.Port.Should().Be(8080);
        info.Protocol.Should().Be(DeckProtocolType.TCP);
        info.HasConflict.Should().BeTrue();
        info.Severity.Should().Be(ConflictSeverity.High);
        info.DetectionTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ProcessInfo_ShouldInitializeCorrectly()
    {
        // Act
        var processInfo = new ProcessInfo
        {
            ProcessId = 1234,
            ProcessName = "test-process",
            IsSystemProcess = false,
            CanBeStopped = true
        };

        // Assert
        processInfo.ProcessId.Should().Be(1234);
        processInfo.ProcessName.Should().Be("test-process");
        processInfo.IsSystemProcess.Should().BeFalse();
        processInfo.CanBeStopped.Should().BeTrue();
    }

    private int GetAvailablePort(int startPort = 20000)
    {
        for (int port = startPort; port < startPort + 1000; port++)
        {
            try
            {
                using var listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                listener.Stop();
                return port;
            }
            catch (SocketException)
            {
                continue;
            }
        }
        throw new InvalidOperationException("无法找到可用的测试端口");
    }

    public void Dispose()
    {
        foreach (var listener in _testListeners)
        {
            try
            {
                listener?.Stop();
            }
            catch
            {
                // 忽略停止监听器时的错误
            }
        }
        _testListeners.Clear();
    }
}