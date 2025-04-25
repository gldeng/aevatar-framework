using System;
using System.Diagnostics.Metrics;
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
                
            _writeDurationHistogram = _meter.CreateHistogram<double>(
                name: "grain.storage.write.duration",
                unit: "ms",
                description: "Duration of grain state write operations");
                
            _clearDurationHistogram = _meter.CreateHistogram<double>(
                name: "grain.storage.clear.duration",
                unit: "ms",
                description: "Duration of grain state clear operations");
                
            // Create counters for operation counts
            _readCounter = _meter.CreateCounter<long>(
                name: "grain.storage.read.count",
                unit: "operations",
                description: "Number of grain state read operations");
                
            _writeCounter = _meter.CreateCounter<long>(
                name: "grain.storage.write.count",
                unit: "operations",
                description: "Number of grain state write operations");
                
            _clearCounter = _meter.CreateCounter<long>(
                name: "grain.storage.clear.count",
                unit: "operations",
                description: "Number of grain state clear operations");
                
            // Create counters for error counts
            _readErrorCounter = _meter.CreateCounter<long>(
                name: "grain.storage.read.errors",
                unit: "errors",
                description: "Number of errors during grain state read operations");
                
            _writeErrorCounter = _meter.CreateCounter<long>(
                name: "grain.storage.write.errors",
                unit: "errors",
                description: "Number of errors during grain state write operations");
                
            _clearErrorCounter = _meter.CreateCounter<long>(
                name: "grain.storage.clear.errors",
                unit: "errors",
                description: "Number of errors during grain state clear operations");
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

        public override async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Increment operation counter
                _writeCounter.Add(1,
                    new KeyValuePair<string, object?>("grainType", typeof(T).Name),
                    new KeyValuePair<string, object?>("stateName", stateName));
                
                await base.WriteStateAsync(stateName, grainId, grainState);
                
                stopwatch.Stop();
                var duration = stopwatch.ElapsedMilliseconds;
                
                // Record duration metric
                _writeDurationHistogram.Record(duration,
                    new KeyValuePair<string, object?>("grainType", typeof(T).Name),
                    new KeyValuePair<string, object?>("stateName", stateName));
                
                _logger.LogTrace("Write state for grain {GrainId} completed in {Duration}ms", 
                    grainId.ToString(), duration);
            }
            catch (Exception ex)
            {
                // Increment error counter
                _writeErrorCounter.Add(1,
                    new KeyValuePair<string, object?>("grainType", typeof(T).Name),
                    new KeyValuePair<string, object?>("stateName", stateName),
                    new KeyValuePair<string, object?>("errorType", ex.GetType().Name));
                
                _logger.LogError(ex, "Error writing state for grain {GrainId}", grainId);
                throw;
            }
        }

        public override async Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Increment operation counter
                _clearCounter.Add(1,
                    new KeyValuePair<string, object?>("grainType", typeof(T).Name),
                    new KeyValuePair<string, object?>("stateName", stateName));
                
                await base.ClearStateAsync(stateName, grainId, grainState);
                
                stopwatch.Stop();
                var duration = stopwatch.ElapsedMilliseconds;
                
                // Record duration metric
                _clearDurationHistogram.Record(duration,
                    new KeyValuePair<string, object?>("grainType", typeof(T).Name),
                    new KeyValuePair<string, object?>("stateName", stateName));
                
                _logger.LogTrace("Clear state for grain {GrainId} completed in {Duration}ms", 
                    grainId.ToString(), duration);
            }
            catch (Exception ex)
            {
                // Increment error counter
                _clearErrorCounter.Add(1,
                    new KeyValuePair<string, object?>("grainType", typeof(T).Name),
                    new KeyValuePair<string, object?>("stateName", stateName),
                    new KeyValuePair<string, object?>("errorType", ex.GetType().Name));
                
                _logger.LogError(ex, "Error clearing state for grain {GrainId}", grainId);
                throw;
            }
        }
    }
} 