using Aevatar.Core.Abstractions;
using Orleans.Streams;

namespace Aevatar.Core;

public interface IGAgentConsumerFactory
{
    IAsyncObserver<EventWrapperBase> CreateConsumer(IReadOnlyList<EventWrapperBaseAsyncObserver> observers, string consumerId);
}

public class GAgentConsumerFactory : IGAgentConsumerFactory
{
    public IAsyncObserver<EventWrapperBase> CreateConsumer(IReadOnlyList<EventWrapperBaseAsyncObserver> observers, string consumerId)
    {
        return new GAgentAsyncObserver(observers.ToList(), consumerId);
    }
}