using Aevatar.Core.Abstractions;
using Orleans.Streams;

namespace Aevatar.Core;

public interface IGAgentConsumerFactory
{
    IAsyncObserver<EventWrapperBase> CreateConsumer(IReadOnlyList<EventWrapperBaseAsyncObserver> observers);
}

public class GAgentConsumerFactory : IGAgentConsumerFactory
{
    public IAsyncObserver<EventWrapperBase> CreateConsumer(IReadOnlyList<EventWrapperBaseAsyncObserver> observers)
    {
        return new GAgentAsyncObserver(observers.ToList());
    }
}