using System;
using System.Threading.Tasks;
using Moq;
using Orleans;
using Orleans.Runtime;
using Orleans.Storage;
using Shouldly;
using Xunit;
using Aevatar.EventSourcing.Core.Storage.Decorators;

namespace Aevatar.EventSourcing.Core.Tests.Storage.Decorators
{
    public class GrainStorageDecoratorBaseTests
    {
        private class TestGrainStorageDecorator : GrainStorageDecoratorBase
        {
            public TestGrainStorageDecorator(IGrainStorage inner) : base(inner)
            {
            }
        }

        [Fact]
        public void Constructor_WithNullInnerStorage_ThrowsArgumentNullException()
        {
            // Act & Assert
            Should.Throw<ArgumentNullException>(() => new TestGrainStorageDecorator(null!));
        }

        [Fact]
        public async Task ReadStateAsync_CallsInnerStorage()
        {
            // Arrange
            var mockStorage = new Mock<IGrainStorage>();
            var decorator = new TestGrainStorageDecorator(mockStorage.Object);
            
            var stateName = "state";
            var grainId = GrainId.Parse("test/123");
            var grainState = new Mock<IGrainState<TestState>>().Object;
            
            // Act
            await decorator.ReadStateAsync(stateName, grainId, grainState);
            
            // Assert
            mockStorage.Verify(s => s.ReadStateAsync(stateName, grainId, grainState), Times.Once);
        }

        [Fact]
        public async Task WriteStateAsync_CallsInnerStorage()
        {
            // Arrange
            var mockStorage = new Mock<IGrainStorage>();
            var decorator = new TestGrainStorageDecorator(mockStorage.Object);
            
            var stateName = "state";
            var grainId = GrainId.Parse("test/123");
            var grainState = new Mock<IGrainState<TestState>>().Object;
            
            // Act
            await decorator.WriteStateAsync(stateName, grainId, grainState);
            
            // Assert
            mockStorage.Verify(s => s.WriteStateAsync(stateName, grainId, grainState), Times.Once);
        }

        [Fact]
        public async Task ClearStateAsync_CallsInnerStorage()
        {
            // Arrange
            var mockStorage = new Mock<IGrainStorage>();
            var decorator = new TestGrainStorageDecorator(mockStorage.Object);
            
            var stateName = "state";
            var grainId = GrainId.Parse("test/123");
            var grainState = new Mock<IGrainState<TestState>>().Object;
            
            // Act
            await decorator.ClearStateAsync(stateName, grainId, grainState);
            
            // Assert
            mockStorage.Verify(s => s.ClearStateAsync(stateName, grainId, grainState), Times.Once);
        }

        public class TestState
        {
            public string? Value { get; set; }
        }
    }
} 