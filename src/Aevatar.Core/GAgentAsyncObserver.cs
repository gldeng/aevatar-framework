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
    
    /// <summary>
    /// Attempts to extract parent context from event metadata
    /// </summary>
    private static ActivityContext? ExtractParentContext(EventWrapperBase item)
    {
        if (item.ContextMetadata == null || item.ContextMetadata.Count == 0)
            return null;
            
        // Try to extract parent context from metadata
        if (!item.ContextMetadata.TryGetValue(EventWrapperBase.TraceIdKey, out var traceIdStr) ||
            !item.ContextMetadata.TryGetValue(EventWrapperBase.SpanIdKey, out var spanIdStr))
            return null;
            
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

            return new ActivityContext(traceId, spanId, traceFlags, isRemote: true);
        }
        catch (Exception)
        {
            // Failed to parse trace ID or span ID
            return null;
        }
    }
    
    /// <summary>
    /// Applies baggage items from event metadata to activity
    /// </summary>
    private static void ApplyBaggageItems(Activity? activity, EventWrapperBase item)
    {
        if (activity == null || item.ContextMetadata == null)
            return;
            
        foreach (var entry in item.ContextMetadata.Where(x => x.Key.StartsWith(EventWrapperBase.BaggagePrefixKey)))
        {
            var baggageKey = entry.Key.Substring(EventWrapperBase.BaggagePrefixKey.Length);
            activity.AddBaggage(baggageKey, entry.Value);
        }
    }
    
    /// <summary>
    /// Creates a tracing activity from the event wrapper if context can be extracted
    /// </summary>
    private Activity? CreateTracingActivity(EventWrapperBase item, EventBase eventType, string? eventId, StreamSequenceToken? token)
    {
        var parentContext = ExtractParentContext(item);
        if (!parentContext.HasValue)
            return null;
            
        // Start activity with extracted parent context and set all standard tags
        var activity = ActivityHelper.StartMessageProcessingActivity(
            eventType.GetType().FullName ?? "UnknownEvent", 
            ActivityKind.Internal, 
            parentContext);
            
        ActivityHelper.SetStandardMessageTags(activity, _grainId, eventType, eventId, token);
        
        // Apply baggage items if any
        ApplyBaggageItems(activity, item);
        
        return activity;
    }
    
    /// <summary>
    /// Finds observers that match the given event type
    /// </summary>
    private List<EventWrapperBaseAsyncObserver> FindMatchingObservers(EventBase eventType)
    {
        return _observers.Where(observer =>
            observer.ParameterTypeName == eventType.GetType().Name ||
            observer.ParameterTypeName == nameof(EventWrapperBase) ||
            observer.MethodName == AevatarGAgentConstants.ForwardEventMethodName ||
            observer.MethodName == AevatarGAgentConstants.ConfigDefaultMethodName).ToList();
    }
    
    /// <summary>
    /// Records an exception in the appropriate tracing context
    /// </summary>
    private void RecordExceptionInTracing(Exception ex, OpenTelemetryScope? scope, Activity? activity)
    {
        if (scope != null)
            scope.RecordException(ex);
        else if (activity != null)
            ActivityHelper.RecordException(activity, ex);
    }
    
    /// <summary>
    /// Processes an event through matching observers with tracing
    /// </summary>
    private async Task ProcessEventThroughObservers(EventWrapperBase item, EventBase eventType)
    {
        var matchedObservers = FindMatchingObservers(eventType);
        foreach (var observer in matchedObservers)
        {
            // TODO: consider adding individual observer-level tracing here
            await observer.OnNextAsync(item);
        }
    }
    
    /// <summary>
    /// Broadcasts a message to all observers
    /// </summary>
    private async Task BroadcastToObservers<T>(Func<EventWrapperBaseAsyncObserver, Task> action)
    {
        foreach (var observer in _observers)
        {
            await action(observer);
        }
    }

    public async Task OnNextAsync(EventWrapperBase item, StreamSequenceToken? token = null)
    {
        // Extract event and ID from wrapper
        var eventType = GetEventWrapperProperty<EventBase>(item, nameof(EventWrapper<EventBase>.Event))!;
        var eventId = GetEventWrapperProperty<object>(item, "EventId")?.ToString();
        
        // Try to create an activity with parent context if available
        var activity = CreateTracingActivity(item, eventType, eventId, token);
        
        // If no activity was created, fall back to the existing scope
        using var scope = activity != null ? 
            null : // We already have an activity from the extracted context
            OpenTelemetryScope.Start(_grainId, eventId, eventType, token);

        try
        {
            await ProcessEventThroughObservers(item, eventType);
        }
        catch (Exception ex)
        {
            RecordExceptionInTracing(ex, scope, activity);
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
        await BroadcastToObservers(observer => observer.OnCompletedAsync());
    }

    public async Task OnErrorAsync(Exception ex)
    {
        await BroadcastToObservers(observer => observer.OnErrorAsync(ex));
    }
}