using Aevatar.Core.Abstractions;

namespace Aevatar.Core;

using System.Diagnostics;
using Orleans.Streams;

internal class OpenTelemetryScope : IDisposable
{
    private static readonly ActivitySource ActivitySource = new ActivitySource("Aevatar.Messaging");

    private readonly string _grainId;

    private Activity _activity;

    public static OpenTelemetryScope Start(string grainId, EventBase? @event, StreamSequenceToken? token = null)
    {
        var obj = new OpenTelemetryScope(grainId);
        obj.StartProcessing(@event, token);
        
        // If there's an active Activity, link it
        if (Activity.Current != null && obj._activity != null)
        {
            obj._activity.SetParentId(Activity.Current.TraceId, Activity.Current.SpanId, Activity.Current.ActivityTraceFlags);
        }
        
        return obj;
    }

    private OpenTelemetryScope(string grainId)
    {
        _grainId = grainId;
    }

    private void StartProcessing(EventBase? @event, StreamSequenceToken? token = null)
    {
        var eventTypeName = @event?.GetType().FullName ?? "UnknownEvent";
        _activity = ActivitySource.StartActivity($"ProcessNextGrainEvent/{eventTypeName}", ActivityKind.Internal);

        // Add standard OpenTelemetry semantic conventions for tags
        _activity?.SetTag("messaging.system", "aevatar");
        _activity?.SetTag("messaging.aevatar.operation", "process");
        _activity?.SetTag("messaging.aevatar.destination_kind", "grain");
        
        // Add OpenTelemetry source/scope metadata
        _activity?.SetTag("otel.scope.name", "Aevatar.Messaging");
        _activity?.SetTag("span.kind", "internal");
        
        // Add event-specific metadata with standard prefixes
        _activity?.SetTag("messaging.correlation_id", @event?.CorrelationId);
        _activity?.SetTag("messaging.event_type", eventTypeName);
        _activity?.SetTag("messaging.aevatar.publisher_grain_id", @event?.PublisherGrainId);
        _activity?.SetTag("messaging.aevatar.consumer_grain_id", _grainId);
        
        if (token != null)
        {
            _activity?.SetTag("messaging.aevatar.sequence_number", token.SequenceNumber.ToString());
        }
        
        // Add timestamp in proper format
        _activity?.SetTag("messaging.timestamp", DateTimeOffset.UtcNow.ToString("o"));
    }

    public void RecordException(Exception ex)
    {
        var errorType = ex.GetType().FullName;

        _activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        _activity?.SetTag("error", true);
        _activity?.SetTag("error.type", errorType);
        _activity?.SetTag("error.message", ex.Message);
        _activity?.SetTag("error.stack_trace", ex.StackTrace);
    }

    public void Dispose()
    {
        _activity?.Stop();
    }
}