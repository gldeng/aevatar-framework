using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Aevatar.EventSourcing.Core.Storage.Decorators;
using Microsoft.Extensions.Logging;
using Moq;
using Orleans;
using Orleans.Runtime;
using Orleans.Storage;
using Shouldly;
using Xunit;

namespace Aevatar.EventSourcing.Core.Tests.Storage.Decorators
{
    public class MetricsGrainStorageTests
    {
        private readonly Mock<IGrainStorage> _mockStorage;
        private readonly Mock<ILogger<MetricsGrainStorage>> _mockLogger;
        private readonly MetricsGrainStorage _metricsStorage;
        
        public MetricsGrainStorageTests()
        {
            _mockStorage = new Mock<IGrainStorage>();
            _mockLogger = new Mock<ILogger<MetricsGrainStorage>>();
            _metricsStorage = new MetricsGrainStorage(_mockStorage.Object, _mockLogger.Object);
        }
        
        [Fact]
        public void Constructor_WithNullInnerStorage_ThrowsArgumentNullException()
        {
            // Act & Assert
            Should.Throw<ArgumentNullException>(() => new MetricsGrainStorage(null!, _mockLogger.Object));
        }
        
        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Should.Throw<ArgumentNullException>(() => new MetricsGrainStorage(_mockStorage.Object, null!));
        }
        
        [Fact]
        public async Task ReadStateAsync_CallsInnerStorage()
        {
            // Arrange
            var stateName = "state";
            var grainId = GrainId.Parse("test/123");
            var grainState = new Mock<IGrainState<TestState>>().Object;
            
            // Act
            await _metricsStorage.ReadStateAsync(stateName, grainId, grainState);
            
            // Assert
            _mockStorage.Verify(s => s.ReadStateAsync(stateName, grainId, grainState), Times.Once);
        }
        
        [Fact]
        public async Task ReadStateAsync_HandlesException_AndRethrows()
        {
            // Arrange
            var stateName = "state";
            var grainId = GrainId.Parse("test/123");
            var grainState = new Mock<IGrainState<TestState>>().Object;
            var exception = new Exception("Test exception");
            
            _mockStorage.Setup(s => s.ReadStateAsync(stateName, grainId, grainState))
                .ThrowsAsync(exception);
            
            // Act & Assert
            var thrownException = await Should.ThrowAsync<Exception>(async () => 
                await _metricsStorage.ReadStateAsync(stateName, grainId, grainState));
            
            thrownException.ShouldBe(exception);
            _mockStorage.Verify(s => s.ReadStateAsync(stateName, grainId, grainState), Times.Once);
        }
        
        [Fact]
        public async Task WriteStateAsync_CallsInnerStorage()
        {
            // Arrange
            var stateName = "state";
            var grainId = GrainId.Parse("test/123");
            var grainState = new Mock<IGrainState<TestState>>().Object;
            
            // Act
            await _metricsStorage.WriteStateAsync(stateName, grainId, grainState);
            
            // Assert
            _mockStorage.Verify(s => s.WriteStateAsync(stateName, grainId, grainState), Times.Once);
        }
        
        [Fact]
        public async Task WriteStateAsync_HandlesException_AndRethrows()
        {
            // Arrange
            var stateName = "state";
            var grainId = GrainId.Parse("test/123");
            var grainState = new Mock<IGrainState<TestState>>().Object;
            var exception = new Exception("Test exception");
            
            _mockStorage.Setup(s => s.WriteStateAsync(stateName, grainId, grainState))
                .ThrowsAsync(exception);
            
            // Act & Assert
            var thrownException = await Should.ThrowAsync<Exception>(async () => 
                await _metricsStorage.WriteStateAsync(stateName, grainId, grainState));
            
            thrownException.ShouldBe(exception);
            _mockStorage.Verify(s => s.WriteStateAsync(stateName, grainId, grainState), Times.Once);
        }
        
        [Fact]
        public async Task ClearStateAsync_CallsInnerStorage()
        {
            // Arrange
            var stateName = "state";
            var grainId = GrainId.Parse("test/123");
            var grainState = new Mock<IGrainState<TestState>>().Object;
            
            // Act
            await _metricsStorage.ClearStateAsync(stateName, grainId, grainState);
            
            // Assert
            _mockStorage.Verify(s => s.ClearStateAsync(stateName, grainId, grainState), Times.Once);
        }
        
        [Fact]
        public async Task ClearStateAsync_HandlesException_AndRethrows()
        {
            // Arrange
            var stateName = "state";
            var grainId = GrainId.Parse("test/123");
            var grainState = new Mock<IGrainState<TestState>>().Object;
            var exception = new Exception("Test exception");
            
            _mockStorage.Setup(s => s.ClearStateAsync(stateName, grainId, grainState))
                .ThrowsAsync(exception);
            
            // Act & Assert
            var thrownException = await Should.ThrowAsync<Exception>(async () => 
                await _metricsStorage.ClearStateAsync(stateName, grainId, grainState));
            
            thrownException.ShouldBe(exception);
            _mockStorage.Verify(s => s.ClearStateAsync(stateName, grainId, grainState), Times.Once);
        }
        
        public class TestState
        {
            public string? Value { get; set; }
        }
    }
} 