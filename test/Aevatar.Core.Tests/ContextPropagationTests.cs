using System.Diagnostics;
using Aevatar.Core.Abstractions;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aevatar.Core.Tests
{
    public class ContextPropagationTests
    {
        [Fact]
        public void ContextMetadata_Is_Added_To_EventWrapper()
        {
            // Arrange
            var rootActivity = new Activity("Root").Start();
            
            try
            {
                // Act
                var testEvent = new TestEvent { CorrelationId = Guid.NewGuid() };
                var eventWrapper = new EventWrapper<TestEvent>(testEvent, Guid.NewGuid(), GrainId.Create("test", "1"));
                
                // Assert
                Assert.NotNull(eventWrapper.ContextMetadata);
                Assert.NotEmpty(eventWrapper.ContextMetadata);
                
                // The TraceId should match the current activity
                Assert.Equal(rootActivity.TraceId.ToString(), eventWrapper.ContextMetadata["TraceId"]);
                Assert.Equal(rootActivity.SpanId.ToString(), eventWrapper.ContextMetadata["SpanId"]);
            }
            finally
            {
                rootActivity.Stop();
            }
        }
        
        [Fact]
        public void Context_Is_Preserved_In_Metadata()
        {
            // Arrange
            var originalActivity = new Activity("SourceActivity").Start();
            originalActivity.AddBaggage("TestKey", "TestValue");
            
            try
            {
                // Create event wrapper which should capture the current activity context
                var testEvent = new TestEvent { CorrelationId = Guid.NewGuid() };
                var eventWrapper = new EventWrapper<TestEvent>(testEvent, Guid.NewGuid(), GrainId.Create("test", "1"));
                
                // Assert
                Assert.NotNull(eventWrapper.ContextMetadata);
                Assert.Equal(originalActivity.TraceId.ToString(), eventWrapper.ContextMetadata["TraceId"]);
                Assert.Equal(originalActivity.SpanId.ToString(), eventWrapper.ContextMetadata["SpanId"]);
                Assert.Equal("TestValue", eventWrapper.ContextMetadata["Baggage.TestKey"]);
            }
            finally
            {
                originalActivity.Stop();
            }
        }
        
        private class TestEvent : EventBase
        {
        }
    }
} 