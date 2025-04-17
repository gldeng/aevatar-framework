using Aevatar.Core.Abstractions;
using Orleans.Streams;
using OrleansCodeGen.Orleans.Runtime;
using System.Diagnostics;

namespace Aevatar.Core;

public class GAgentAsyncObserver : IAsyncObserver<EventWrapperBase>
{
    private readonly List<EventWrapperBaseAsyncObserver> _observers;
    private readonly string _grainId;

    public GAgentAsyncObserver(List<EventWrapperBaseAsyncObserver> observers, string grainId)
    {
        _observers = observers;
        _grainId = grainId;
    }
    
    /// <summary>
    /// Helper method to extract a property from an EventWrapper using reflection
    /// </summary>
    private static T? GetEventWrapperProperty<T>(EventWrapperBase wrapper, string propertyName) where T : class
    {
        return wrapper.GetType().GetProperty(propertyName)?.GetValue(wrapper) as T;
    }
    
    public async Task OnNextAsync(EventWrapperBase item, StreamSequenceToken? token = null)
    {
        var eventType = GetEventWrapperProperty<EventBase>(item, nameof(EventWrapper<EventBase>.Event))!;
        
        // Extract EventId from the wrapper using reflection
        var eventId = GetEventWrapperProperty<object>(item, "EventId")?.ToString();
        
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
                    
                    // Start activity with extracted parent context and set all standard tags
                    activity = ActivityHelper.StartMessageProcessingActivity(
                        eventType.GetType().FullName ?? "UnknownEvent", 
                        ActivityKind.Internal, 
                        parentContext);
                        
                    ActivityHelper.SetStandardMessageTags(activity, _grainId, eventType, eventId, token);
                    
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
            OpenTelemetryScope.Start(_grainId, eventId, eventType, token);

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
                ActivityHelper.RecordException(activity, ex);
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