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
            if (item.ContextMetadata.TryGetValue("TraceId", out var traceIdStr) &&
                item.ContextMetadata.TryGetValue("SpanId", out var spanIdStr))
            {
                try
                {
                    // Parse trace ID and span ID
                    var traceId = ActivityTraceId.CreateFromString(traceIdStr);
                    var spanId = ActivitySpanId.CreateFromString(spanIdStr);
                    
                    // Parse trace flags if available
                    ActivityTraceFlags traceFlags = ActivityTraceFlags.None;
                    if (item.ContextMetadata.TryGetValue("TraceFlags", out var traceFlagsStr))
                    {
                        Enum.TryParse(traceFlagsStr, out traceFlags);
                    }

                    var parentContext = new ActivityContext(traceId, spanId, traceFlags, isRemote: true);
                    
                    // Start activity with extracted parent context
                    activity = ActivitySource.StartActivity(
                        $"ProcessNextGrainEvent/{eventType.GetType().FullName}",
                        ActivityKind.Internal,
                        parentContext);
                    
                    // Apply baggage items if any
                    foreach (var entry in item.ContextMetadata.Where(x => x.Key.StartsWith("Baggage.")))
                    {
                        var baggageKey = entry.Key.Substring("Baggage.".Length);
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
                activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                
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