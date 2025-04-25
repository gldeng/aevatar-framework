using System;
using Aevatar.EventSourcing.Core.Storage.Decorators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Storage;
using Scrutor;

namespace Aevatar.EventSourcing.Core.Extensions
{
    /// <summary>
    /// Extension methods for registering grain storage decorators
    /// </summary>
    public static class GrainStorageDecoratorExtensions
    {
        /// <summary>
        /// Adds decorator registration support to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddGrainStorageDecorators(this IServiceCollection services)
        {
            // Make sure Scrutor is available
            services.Scan(scan => { });
            
            return services;
        }
        
        /// <summary>
        /// Adds the metrics decorator for IGrainStorage
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddMetricsGrainStorage(this IServiceCollection services)
        {
            services.Decorate<IGrainStorage>((inner, provider) => 
                new MetricsGrainStorage(
                    inner, 
                    provider.GetRequiredService<ILogger<MetricsGrainStorage>>()));
                
            return services;
        }
    }
}