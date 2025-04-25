# AEVT-001: IGrainStorage Metrics Decorator Implementation with Scrutor

## User Story

**As a** developer working with the Aevatar Framework  
**I want to** be able to decorate the IGrainStorage implementation with performance monitoring capabilities  
**So that** I can collect metrics about storage operations without modifying the original implementation

**Acceptance Criteria:**
1. Implement a base decorator pattern for IGrainStorage using Scrutor
2. Create a metrics-focused decorator that implements IGrainStorage
3. Collect performance metrics for Read/Write/Clear operations
4. Support integration with existing telemetry systems
5. Provide extension methods for easy registration in DI container
6. Include comprehensive unit tests
7. Document the usage patterns and examples

## Background

The Orleans `IGrainStorage` interface is a critical component of the Aevatar Framework's persistence layer, responsible for reading, writing, and clearing grain state. Currently, it's not possible to gather performance metrics for this interface without modifying the underlying implementation.

Implementing the decorator pattern will allow us to:
- Monitor performance to identify bottlenecks
- Track operation durations
- Collect usage statistics
- Identify problematic grains or storage patterns

## Technical Design

### 1. Package Dependencies

Add Scrutor package to the project to enable decorator registration in the DI container:

```xml
<PackageVersion Include="Scrutor" Version="4.2.2" />
```

### 2. Core Components

#### Base IGrainStorage Decorator

```csharp
namespace Aevatar.EventSourcing.Core.Storage.Decorators
{
    public abstract class GrainStorageDecoratorBase : IGrainStorage
    {
        protected readonly IGrainStorage _inner;

        protected GrainStorageDecoratorBase(IGrainStorage inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public virtual Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            return _inner.ReadStateAsync<T>(stateName, grainId, grainState);
        }

        public virtual Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            return _inner.WriteStateAsync<T>(stateName, grainId, grainState);
        }

        public virtual Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            return _inner.ClearStateAsync<T>(stateName, grainId, grainState);
        }
    }
}
```

#### Performance Monitoring Decorator

```csharp
namespace Aevatar.EventSourcing.Core.Storage.Decorators
{
    public class MetricsGrainStorage : GrainStorageDecoratorBase
    {
        private readonly ILogger<MetricsGrainStorage> _logger;
        private readonly Meter _meter;
        private readonly Histogram<double> _readDurationHistogram;
        private readonly Histogram<double> _writeDurationHistogram;
        private readonly Histogram<double> _clearDurationHistogram;
        private readonly Counter<long> _readCounter;
        private readonly Counter<long> _writeCounter;
        private readonly Counter<long> _clearCounter;
        private readonly Counter<long> _readErrorCounter;
        private readonly Counter<long> _writeErrorCounter;
        private readonly Counter<long> _clearErrorCounter;

        private const string MeterName = "Aevatar.Storage.Metrics";

        public MetricsGrainStorage(
            IGrainStorage inner,
            ILogger<MetricsGrainStorage> logger) : base(inner)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Initialize metrics
            _meter = new Meter(MeterName, "1.0.0");
            
            // Create histograms for operation durations
            _readDurationHistogram = _meter.CreateHistogram<double>(
                name: "grain.storage.read.duration",
                unit: "ms",
                description: "Duration of grain state read operations");
                
            // Additional metrics initialization...
        }

        public override async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Increment operation counter
                _readCounter.Add(1, 
                    new KeyValuePair<string, object?>("grainType", typeof(T).Name),
                    new KeyValuePair<string, object?>("stateName", stateName));

                await base.ReadStateAsync(stateName, grainId, grainState);
                
                stopwatch.Stop();
                var duration = stopwatch.ElapsedMilliseconds;
                
                // Record duration metric
                _readDurationHistogram.Record(duration, 
                    new KeyValuePair<string, object?>("grainType", typeof(T).Name),
                    new KeyValuePair<string, object?>("stateName", stateName));
                
                _logger.LogTrace("Read state for grain {GrainId} completed in {Duration}ms", 
                    grainId.ToString(), duration);
            }
            catch (Exception ex)
            {
                // Increment error counter
                _readErrorCounter.Add(1,
                    new KeyValuePair<string, object?>("grainType", typeof(T).Name),
                    new KeyValuePair<string, object?>("stateName", stateName),
                    new KeyValuePair<string, object?>("errorType", ex.GetType().Name));
                
                _logger.LogError(ex, "Error reading state for grain {GrainId}", grainId);
                throw;
            }
        }

        // Similar implementations for WriteStateAsync and ClearStateAsync
    }
}
```

### 3. Registration Extensions

```csharp
namespace Aevatar.EventSourcing.Core.Extensions
{
    public static class GrainStorageDecoratorExtensions
    {
        public static IServiceCollection AddGrainStorageDecorators(this IServiceCollection services)
        {
            // Make sure Scrutor is available
            services.Scan(scan => { });
            
            return services;
        }
        
        public static IServiceCollection AddMetricsGrainStorage(this IServiceCollection services)
        {
            services.Decorate<IGrainStorage>((inner, provider) => 
                new MetricsGrainStorage(
                    inner, 
                    provider.GetRequiredService<ILogger<MetricsGrainStorage>>()));
                
            return services;
        }
    }
}
```

### 4. Metrics Collection

The MetricsGrainStorage decorator uses the .NET System.Diagnostics.Metrics API for collecting performance metrics. This approach provides several benefits:

1. **Standard Metrics API**: Using the standard .NET metrics API ensures compatibility with various monitoring systems.
2. **Focused Metrics Collection**: The implementation focuses purely on metrics collection without the overhead of distributed tracing.
3. **Dimensional Metrics**: Each metric includes dimensions (tags) such as grain type and state name for detailed analysis.
4. **Performance Optimized**: Minimal overhead for high-throughput grain operations.

The following metrics are collected:

| Metric | Type | Description | Tags |
|--------|------|-------------|------|
| grain_storage_read_count | Counter | Number of read operations | grainType, stateName |
| grain_storage_write_count | Counter | Number of write operations | grainType, stateName |
| grain_storage_clear_count | Counter | Number of clear operations | grainType, stateName |
| grain_storage_read_duration | Histogram | Duration of read operations in ms | grainType, stateName |
| grain_storage_write_duration | Histogram | Duration of write operations in ms | grainType, stateName |
| grain_storage_clear_duration | Histogram | Duration of clear operations in ms | grainType, stateName |
| grain_storage_read_errors | Counter | Number of read errors | grainType, stateName, errorType |
| grain_storage_write_errors | Counter | Number of write errors | grainType, stateName, errorType |
| grain_storage_clear_errors | Counter | Number of clear errors | grainType, stateName, errorType |

### 5. Integration with Monitoring Systems

The metrics collected can be consumed by various monitoring systems:

1. **Prometheus**: Using the prometheus-net.DotNetRuntime package
2. **Application Insights**: Using the Microsoft.ApplicationInsights.AspNetCore package
3. **OpenTelemetry**: Using the OpenTelemetry.Exporter packages

Example configuration for Prometheus:

```csharp
services.AddSingleton<IHostedService, PrometheusMetricsService>();
```

Example configuration for OpenTelemetry:

```csharp
services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddMeter("Aevatar_Storage")
        .AddPrometheusExporter());
```

### 6. Usage Example

```csharp
// In Startup.cs or Program.cs

public void ConfigureServices(IServiceCollection services)
{
    // Register the base IGrainStorage implementation
    services.AddTransient<IGrainStorage, MongoDBGrainStorage>();
    
    // Add metrics decorator
    services.AddMetricsGrainStorage();
    
    // This results in: 
    // MetricsGrainStorage -> MongoDBGrainStorage
    
    // Add metrics collection
    services.AddOpenTelemetry()
        .WithMetrics(metrics => metrics
            .AddMeter("Aevatar_Storage")
            .AddConsoleExporter());
}
```

## Implementation Plan

1. **Phase 1: Core Implementation**
   - Add Scrutor NuGet package
   - Implement GrainStorageDecoratorBase
   - Implement MetricsGrainStorage decorator
   - Write unit tests for basic functionality

2. **Phase 2: Integration and Documentation**
   - Create extension methods for easy registration
   - Write integration tests
   - Create documentation and usage examples
   - Update dependency injection in sample applications

## Testing Strategy

1. **Unit Testing**
   - Test the metrics decorator in isolation with mocked inner storage
   - Verify decorated methods call inner methods
   - Verify metrics are collected correctly
   - Test error handling scenarios

2. **Integration Testing**
   - Test with actual storage implementations
   - Verify proper registration through DI container
   - Ensure metrics are captured in the telemetry system

3. **Performance Testing**
   - Verify monitoring decorator doesn't add significant overhead
   - Test with various payload sizes and operation types

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Decorator could degrade performance | Medium | Thorough performance testing, optimize metrics collection |
| Telemetry integration might vary across environments | Medium | Create abstraction for telemetry to support different providers |
| Potential circular dependencies in DI | High | Careful registration order, unit tests for DI setup |

## Benefits

1. **Improved Diagnostics**: Gain visibility into storage performance
2. **Identify Bottlenecks**: Find slow operations or problematic grains
3. **Separation of Concerns**: Cleanly separate metrics collection from core storage logic
4. **Foundation for Extensibility**: Create a pattern for future decorators (caching, logging)
5. **Non-invasive**: Add metrics without modifying existing storage implementation

## Future Work

This ticket implements the base decorator pattern and metrics collection. Future tickets will implement:

1. Caching decorator for improved performance
2. Logging decorator for better diagnostics
3. Additional specialized decorators as needed

## References

1. [Scrutor GitHub Repository](https://github.com/khellang/Scrutor)
2. [Microsoft Documentation on the Decorator Pattern](https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/microservice-ddd-cqrs-patterns/implement-decorator-pattern)
3. [Orleans Documentation - Grain Persistence](https://learn.microsoft.com/en-us/dotnet/orleans/implementation/grain-persistence) 