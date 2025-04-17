using Aevatar.Core.Abstractions;

namespace Aevatar.Core;

using System.Diagnostics;
using Orleans.Streams;

internal class OpenTelemetryScope : IDisposable
{
    private static readonly ActivitySource ActivitySource = new ActivitySource(OpenTelemetryConstants.ActivitySourceName);

    private readonly string _grainId;
    private readonly string? _eventId;

    private Activity _activity;

    public static OpenTelemetryScope Start(string grainId, string? eventId, EventBase? @event, StreamSequenceToken? token = null)
    {
        var obj = new OpenTelemetryScope(grainId, eventId);
        obj.StartProcessing(@event, token);
        
        // If there's an active Activity, link it
        if (Activity.Current != null && obj._activity != null)
        {
            obj._activity.SetParentId(Activity.Current.TraceId, Activity.Current.SpanId, Activity.Current.ActivityTraceFlags);
        }
        
        return obj;
    }

    private OpenTelemetryScope(string grainId, string? eventId)
    {
        _grainId = grainId;
        _eventId = eventId;
    }

    private void StartProcessing(EventBase? @event, StreamSequenceToken? token = null)
    {
        var eventTypeName = @event?.GetType().FullName ?? "UnknownEvent";
        _activity = ActivitySource.StartActivity(
            $"{OpenTelemetryConstants.MessageProcessSpanNamePrefix}/{eventTypeName}", 
            ActivityKind.Internal);

        // Add standard OpenTelemetry semantic conventions for tags
        _activity?.SetTag(OpenTelemetryConstants.MessagingSystemTag, OpenTelemetryConstants.AevatarSystem);
        _activity?.SetTag(OpenTelemetryConstants.OperationTag, OpenTelemetryConstants.ProcessOperation);
        _activity?.SetTag(OpenTelemetryConstants.DestinationKindTag, OpenTelemetryConstants.GrainDestination);
        
        // Add OpenTelemetry source/scope metadata
        _activity?.SetTag(OpenTelemetryConstants.ScopeNameTag, OpenTelemetryConstants.ActivitySourceName);
        _activity?.SetTag(OpenTelemetryConstants.SpanKindTag, OpenTelemetryConstants.InternalSpanKind);
        
        // Add event-specific metadata with standard prefixes
        _activity?.SetTag(OpenTelemetryConstants.CorrelationIdTag, @event?.CorrelationId);
        _activity?.SetTag(OpenTelemetryConstants.EventIdTag, _eventId);
        _activity?.SetTag(OpenTelemetryConstants.EventTypeTag, eventTypeName);
        _activity?.SetTag(OpenTelemetryConstants.PublisherGrainIdTag, @event?.PublisherGrainId);
        _activity?.SetTag(OpenTelemetryConstants.ConsumerGrainIdTag, _grainId);
        
        if (token != null)
        {
            _activity?.SetTag(OpenTelemetryConstants.SequenceNumberTag, token.SequenceNumber.ToString());
        }
        
        // Add timestamp in proper format
        _activity?.SetTag(OpenTelemetryConstants.TimestampTag, DateTimeOffset.UtcNow.ToString("o"));
    }

    public void RecordException(Exception ex)
    {
        var errorType = ex.GetType().FullName;

        _activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        _activity?.SetTag(OpenTelemetryConstants.ErrorTag, true);
        _activity?.SetTag(OpenTelemetryConstants.ErrorTypeTag, errorType);
        _activity?.SetTag(OpenTelemetryConstants.ErrorMessageTag, ex.Message);
        _activity?.SetTag(OpenTelemetryConstants.ErrorStackTraceTag, ex.StackTrace);
    }

    public void Dispose()
    {
        _activity?.Stop();
    }
}