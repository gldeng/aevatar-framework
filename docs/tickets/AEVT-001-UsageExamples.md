# AEVT-001: IGrainStorage Metrics Decorator Usage Examples

This document provides examples of how to use the IGrainStorage metrics decorator in your Aevatar Framework application.

## Basic Usage

The metrics decorator uses the decorator pattern to wrap any IGrainStorage implementation with metrics collection capabilities. This allows you to monitor performance without modifying the original storage provider implementation.

### Registration in DI Container

To add the metrics decorator to your application, use the extension methods provided:

```csharp
// In Program.cs or Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // First, register your IGrainStorage implementation
    services.AddTransient<IGrainStorage, YourStorageProvider>();
    
    // Then add the metrics decorator
    services.AddMetricsGrainStorage();
}
```

The decorator will automatically wrap the registered IGrainStorage implementation and collect metrics for all storage operations.

### Usage with Orleans Silo

When using with Orleans, you can register the decorator in your silo configuration:

```csharp
var builder = new SiloBuilder()
    .ConfigureServices(services =>
    {
        // Add your storage provider
        services.AddMongoDbBasedLogConsistencyProviderAsDefault(options =>
        {
            options.ConnectionString = "mongodb://localhost:27017";
            options.DatabaseName = "OrleansStorage";
        });
        
        // Add the metrics decorator
        services.AddMetricsGrainStorage();
    });
```

## Advanced Configuration

### Multiple Storage Providers

If your application uses multiple storage providers, the decorator will be applied to all of them:

```csharp
services.AddKeyedSingleton<IGrainStorage>("Provider1", sp => new Provider1());
services.AddKeyedSingleton<IGrainStorage>("Provider2", sp => new Provider2());

// Add the metrics decorator - will apply to both providers
services.AddMetricsGrainStorage();
```

### Customizing Metrics Collection

The metrics decorator uses System.Diagnostics.ActivitySource for telemetry. You can configure an ActivityListener to capture and process these metrics:

```csharp
// Configure ActivitySource listener
using var listener = new ActivityListener
{
    ShouldListenTo = source => source.Name == "GrainStorage",
    Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
    ActivityStarted = activity => Console.WriteLine($"Started: {activity.OperationName}"),
    ActivityStopped = activity => 
    {
        Console.WriteLine($"Stopped: {activity.OperationName}, Duration: {activity.Duration}");
        foreach (var tag in activity.Tags)
        {
            Console.WriteLine($"  {tag.Key}: {tag.Value}");
        }
    }
};

ActivitySource.AddActivityListener(listener);
```

### Integration with Application Monitoring

The metrics can be integrated with your existing monitoring infrastructure:

```csharp
// Example with Application Insights
services.AddApplicationInsightsTelemetry();

// The ActivitySource-based metrics will automatically flow to Application Insights
```

## Performance Considerations

The metrics decorator adds minimal overhead to storage operations. However, in extremely performance-sensitive scenarios, you might want to:

1. Use sampling to reduce the number of metrics collected
2. Apply the decorator only in specific environments (e.g., staging but not production)
3. Filter which operations are measured based on grain types or state names

## Troubleshooting

If you're not seeing metrics being collected:

1. Verify the decorator is registered after the storage provider
2. Ensure your ActivityListener is properly configured
3. Check that you're using the decorated IGrainStorage instance (from DI)

## Example: Monitoring Storage Performance

This example shows how to use the metrics decorator to identify slow storage operations:

```csharp
// Configure a listener that logs operations that take longer than 100ms
using var listener = new ActivityListener
{
    ShouldListenTo = source => source.Name == "GrainStorage",
    Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
    ActivityStopped = activity => 
    {
        if (activity.Duration > TimeSpan.FromMilliseconds(100))
        {
            _logger.LogWarning(
                "Slow storage operation: {Operation} for grain {GrainId}, took {Duration}ms",
                activity.OperationName,
                activity.GetTagItem("GrainId"),
                activity.Duration.TotalMilliseconds);
        }
    }
};

ActivitySource.AddActivityListener(listener);
```

## Additional Resources

- [Orleans Documentation on Storage Providers](https://learn.microsoft.com/en-us/dotnet/orleans/host/configuration-guide/storage-providers)
- [System.Diagnostics.Activity Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity) 