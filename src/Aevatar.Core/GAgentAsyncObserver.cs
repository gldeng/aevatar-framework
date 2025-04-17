using Aevatar.Core.Abstractions;
using Orleans.Streams;
using OrleansCodeGen.Orleans.Runtime;
using System.Diagnostics;

namespace Aevatar.Core;

public class GAgentAsyncObserver : IAsyncObserver<EventWrapperBase>
{
    private readonly List<EventWrapperBaseAsyncObserver> _observers;
    private readonly string _grainId;
    private static readonly ActivitySource ActivitySource = new ActivitySource("Aevatar.Messaging");

    public GAgentAsyncObserver(List<EventWrapperBaseAsyncObserver> observers, string grainId)
    {
        _observers = observers;
        _grainId = grainId;
    }
    
    public async Task OnNextAsync(EventWrapperBase item, StreamSequenceToken? token = null)
    {
        var eventType = (EventBase)item.GetType().GetProperty(nameof(EventWrapper<EventBase>.Event))?.GetValue(item)!;
        
        // Extract context from the event wrapper
        Activity? activity = null;
        if (item.ContextMetadata != null && item.ContextMetadata.Count > 0)
        {
            // Try to extract parent context from metadata
            if (item.ContextMetadata.TryGetValue(EventWrapperBase.TraceIdKey, out var traceIdStr) &&
                item.ContextMetadata.TryGetValue(EventWrapperBase.SpanIdKey, out var spanIdStr))
            {
                try
                {
                    // Parse trace ID and span ID
                    var traceId = ActivityTraceId.CreateFromString(traceIdStr);
                    var spanId = ActivitySpanId.CreateFromString(spanIdStr);
                    
                    // Parse trace flags if available
                    ActivityTraceFlags traceFlags = ActivityTraceFlags.None;
                    if (item.ContextMetadata.TryGetValue(EventWrapperBase.TraceFlagsKey, out var traceFlagsStr))
                    {
                        Enum.TryParse(traceFlagsStr, out traceFlags);
                    }

                    var parentContext = new ActivityContext(traceId, spanId, traceFlags, isRemote: true);
                    
                    // Start activity with extracted parent context
                    activity = ActivitySource.StartActivity(
                        $"aevatar.message.process/{eventType.GetType().FullName}",
                        ActivityKind.Internal,
                        parentContext);
                    
                    // Add standard OpenTelemetry semantic conventions for tags
                    activity?.SetTag("messaging.system", "aevatar");
                    activity?.SetTag("messaging.aevatar.operation", "process");
                    activity?.SetTag("messaging.aevatar.destination_kind", "grain");
                    
                    // Add OpenTelemetry source/scope metadata
                    activity?.SetTag("otel.scope.name", "Aevatar.Messaging");
                    activity?.SetTag("span.kind", "internal");
                    
                    // Add event-specific metadata with standard prefixes
                    activity?.SetTag("messaging.aevatar.correlation_id", eventType.CorrelationId);
                    activity?.SetTag("messaging.aevatar.event_id", item.GetType().GetProperty("EventId")?.GetValue(item));
                    activity?.SetTag("messaging.aevatar.event_type", eventType.GetType().FullName);
                    activity?.SetTag("messaging.aevatar.publisher_grain_id", eventType.PublisherGrainId);
                    activity?.SetTag("messaging.aevatar.consumer_grain_id", _grainId);
                    
                    if (token != null)
                    {
                        activity?.SetTag("messaging.aevatar.sequence_number", token.SequenceNumber.ToString());
                    }
                    
                    // Add timestamp in proper format
                    activity?.SetTag("messaging.timestamp", DateTimeOffset.UtcNow.ToString("o"));
                    
                    // Apply baggage items if any
                    foreach (var entry in item.ContextMetadata.Where(x => x.Key.StartsWith(EventWrapperBase.BaggagePrefixKey)))
                    {
                        var baggageKey = entry.Key.Substring(EventWrapperBase.BaggagePrefixKey.Length);
                        activity?.AddBaggage(baggageKey, entry.Value);
                    }
                }
                catch (Exception)
                {
                    // Failed to parse trace ID or span ID
                    activity = null;
                }
            }
        }
        
        // If no context was extracted, fall back to the existing scope
        using var scope = activity != null ? 
            null : // We already have an activity from the extracted context
            OpenTelemetryScope.Start(_grainId, eventType, token);

        try
        {
            // TODO: Maybe use RuleEngine to optimize this.
            var matchedObservers = _observers.Where(observer =>
                observer.ParameterTypeName == eventType.GetType().Name ||
                observer.ParameterTypeName == nameof(EventWrapperBase) ||
                observer.MethodName == AevatarGAgentConstants.ForwardEventMethodName ||
                observer.MethodName == AevatarGAgentConstants.ConfigDefaultMethodName).ToList();
            foreach (var observer in matchedObservers)
            {
                // TODO: add tracing for individual observer
                await observer.OnNextAsync(item);
            }            
        }
        catch (Exception ex)
        {
            if (scope != null)
                scope.RecordException(ex);
            else if (activity != null)
            {
                activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity.SetTag("error", true);
                activity.SetTag("error.type", ex.GetType().FullName);
                activity.SetTag("error.message", ex.Message);
                activity.SetTag("error.stack_trace", ex.StackTrace);
            }
                
            throw;
        }
        finally
        {
            // Dispose of the activity if we created one
            activity?.Dispose();
        }
    }

    public async Task OnCompletedAsync()
    {
        foreach (var observer in _observers)
        {
            await observer.OnCompletedAsync();
        }
    }

    public async Task OnErrorAsync(Exception ex)
    {
        foreach (var observer in _observers)
        {
            await observer.OnErrorAsync(ex);
        }
    }
}