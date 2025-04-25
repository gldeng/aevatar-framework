using System;
using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime;
using Orleans.Storage;

namespace Aevatar.EventSourcing.Core.Storage.Decorators
{
    /// <summary>
    /// Base decorator for IGrainStorage implementations
    /// </summary>
    public abstract class GrainStorageDecoratorBase : IGrainStorage
    {
        protected readonly IGrainStorage _inner;

        protected GrainStorageDecoratorBase(IGrainStorage inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public virtual Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            return _inner.ReadStateAsync<T>(stateName, grainId, grainState);
        }

        public virtual Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            return _inner.WriteStateAsync<T>(stateName, grainId, grainState);
        }

        public virtual Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            return _inner.ClearStateAsync<T>(stateName, grainId, grainState);
        }
    }
} 