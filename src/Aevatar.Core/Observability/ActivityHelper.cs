using Aevatar.Core.Abstractions;
using System.Diagnostics;
using Orleans.Streams;

namespace Aevatar.Core;

/// <summary>
/// Helper class for Activity (span) management and tag standardization
/// </summary>
internal static class ActivityHelper
{
    private static readonly ActivitySource ActivitySource = new ActivitySource(OpenTelemetryConstants.ActivitySourceName);
    
    /// <summary>
    /// Creates a new Activity for processing an event
    /// </summary>
    public static Activity? StartMessageProcessingActivity(
        string eventTypeName,
        ActivityKind kind = ActivityKind.Internal,
        ActivityContext? parentContext = null)
    {
        var name = $"{OpenTelemetryConstants.MessageProcessSpanNamePrefix}/{eventTypeName}";
        
        // Create activity with appropriate parent context
        return parentContext.HasValue
            ? ActivitySource.StartActivity(name, kind, parentContext.Value)
            : ActivitySource.StartActivity(name, kind);
    }
    
    /// <summary>
    /// Sets all standard tags for a messaging event on an Activity
    /// </summary>
    public static void SetStandardMessageTags(
        Activity? activity,
        string consumerGrainId,
        EventBase? @event = null,
        string? eventId = null,
        StreamSequenceToken? token = null)
    {
        if (activity == null) return;
        
        // Set system and operation tags
        activity.SetTag(OpenTelemetryConstants.MessagingSystemTag, OpenTelemetryConstants.AevatarSystem);
        activity.SetTag(OpenTelemetryConstants.OperationTag, OpenTelemetryConstants.ProcessOperation);
        activity.SetTag(OpenTelemetryConstants.DestinationKindTag, OpenTelemetryConstants.GrainDestination);
        
        // Set OpenTelemetry metadata
        activity.SetTag(OpenTelemetryConstants.ScopeNameTag, OpenTelemetryConstants.ActivitySourceName);
        activity.SetTag(OpenTelemetryConstants.SpanKindTag, OpenTelemetryConstants.InternalSpanKind);
        
        // Set event metadata if available
        if (@event != null)
        {
            activity.SetTag(OpenTelemetryConstants.CorrelationIdTag, @event.CorrelationId);
            activity.SetTag(OpenTelemetryConstants.EventTypeTag, @event.GetType().FullName);
            activity.SetTag(OpenTelemetryConstants.PublisherGrainIdTag, @event.PublisherGrainId);
        }
        
        // Set EventId if available
        if (!string.IsNullOrEmpty(eventId))
        {
            activity.SetTag(OpenTelemetryConstants.EventIdTag, eventId);
        }
        
        // Set consumer grain id
        activity.SetTag(OpenTelemetryConstants.ConsumerGrainIdTag, consumerGrainId);
        
        // Set sequence number if available
        if (token != null)
        {
            activity.SetTag(OpenTelemetryConstants.SequenceNumberTag, token.SequenceNumber.ToString());
        }
        
        // Add timestamp
        activity.SetTag(OpenTelemetryConstants.TimestampTag, DateTimeOffset.UtcNow.ToString("o"));
    }
    
    /// <summary>
    /// Records exception details on an Activity
    /// </summary>
    public static void RecordException(Activity? activity, Exception ex)
    {
        if (activity == null) return;
        
        activity.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity.SetTag(OpenTelemetryConstants.ErrorTag, true);
        activity.SetTag(OpenTelemetryConstants.ErrorTypeTag, ex.GetType().FullName);
        activity.SetTag(OpenTelemetryConstants.ErrorMessageTag, ex.Message);
        activity.SetTag(OpenTelemetryConstants.ErrorStackTraceTag, ex.StackTrace);
    }
} 