using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Orleans.Storage;

namespace Aevatar.EventSourcing.Core.Storage.Decorators
{
    /// <summary>
    /// A decorator for IGrainStorage that collects performance metrics
    /// </summary>
    public class MetricsGrainStorage : GrainStorageDecoratorBase
    {
        private readonly ILogger<MetricsGrainStorage> _logger;
        private readonly ActivitySource _activitySource;
        private const string ActivitySourceName = "Aevatar.Storage";

        public MetricsGrainStorage(
            IGrainStorage inner,
            ILogger<MetricsGrainStorage> logger) : base(inner)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _activitySource = new ActivitySource(ActivitySourceName);
        }

        public override async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            using var activity = _activitySource.StartActivity("GrainStorage.ReadState", ActivityKind.Internal);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await base.ReadStateAsync(stateName, grainId, grainState);
                
                stopwatch.Stop();
                
                if (activity != null)
                {
                    activity.SetTag("GrainId", grainId.ToString());
                    activity.SetTag("StateName", stateName);
                    activity.SetTag("GrainType", typeof(T).Name);
                    activity.SetTag("Duration", stopwatch.ElapsedMilliseconds);
                }
                
                _logger.LogTrace("Read state for grain {GrainId} completed in {Duration}ms", 
                    grainId.ToString(), stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.SetTag("Error", ex.Message);
                activity?.SetTag("ErrorType", ex.GetType().FullName);
                
                _logger.LogError(ex, "Error reading state for grain {GrainId}", grainId);
                throw;
            }
        }

        public override async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            using var activity = _activitySource.StartActivity("GrainStorage.WriteState", ActivityKind.Internal);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await base.WriteStateAsync(stateName, grainId, grainState);
                
                stopwatch.Stop();
                
                if (activity != null)
                {
                    activity.SetTag("GrainId", grainId.ToString());
                    activity.SetTag("StateName", stateName);
                    activity.SetTag("GrainType", typeof(T).Name);
                    activity.SetTag("Duration", stopwatch.ElapsedMilliseconds);
                }
                
                _logger.LogTrace("Write state for grain {GrainId} completed in {Duration}ms", 
                    grainId.ToString(), stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.SetTag("Error", ex.Message);
                activity?.SetTag("ErrorType", ex.GetType().FullName);
                
                _logger.LogError(ex, "Error writing state for grain {GrainId}", grainId);
                throw;
            }
        }

        public override async Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            using var activity = _activitySource.StartActivity("GrainStorage.ClearState", ActivityKind.Internal);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await base.ClearStateAsync(stateName, grainId, grainState);
                
                stopwatch.Stop();
                
                if (activity != null)
                {
                    activity.SetTag("GrainId", grainId.ToString());
                    activity.SetTag("StateName", stateName);
                    activity.SetTag("GrainType", typeof(T).Name);
                    activity.SetTag("Duration", stopwatch.ElapsedMilliseconds);
                }
                
                _logger.LogTrace("Clear state for grain {GrainId} completed in {Duration}ms", 
                    grainId.ToString(), stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.SetTag("Error", ex.Message);
                activity?.SetTag("ErrorType", ex.GetType().FullName);
                
                _logger.LogError(ex, "Error clearing state for grain {GrainId}", grainId);
                throw;
            }
        }
    }
} 