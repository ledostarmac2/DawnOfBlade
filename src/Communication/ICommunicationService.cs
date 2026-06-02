using System;
using System.Threading;
using System.Threading.Tasks;

namespace DawnOfBlade.Communication;

/// <summary>
/// Boundary for communication between gameplay services. The local implementation dispatches
/// in-process; a future server adapter can carry the same envelopes across a transport.
/// </summary>
public interface ICommunicationService
{
    IDisposable Subscribe<TEvent>(
        Func<MessageEnvelope<TEvent>, CancellationToken, ValueTask> handler)
        where TEvent : IEvent;

    IDisposable RegisterHandler<TRequest, TResponse>(
        Func<MessageEnvelope<TRequest>, CancellationToken, ValueTask<TResponse>> handler)
        where TRequest : IRequest<TResponse>;

    ValueTask PublishAsync<TEvent>(
        TEvent message,
        Guid? correlationId = null,
        Guid? causationId = null,
        CancellationToken cancellationToken = default)
        where TEvent : IEvent;

    ValueTask PublishAsync<TEvent>(
        MessageEnvelope<TEvent> envelope,
        CancellationToken cancellationToken = default)
        where TEvent : IEvent;

    ValueTask<TResponse> SendAsync<TRequest, TResponse>(
        TRequest message,
        Guid? correlationId = null,
        Guid? causationId = null,
        CancellationToken cancellationToken = default)
        where TRequest : IRequest<TResponse>;

    ValueTask<TResponse> SendAsync<TRequest, TResponse>(
        MessageEnvelope<TRequest> envelope,
        CancellationToken cancellationToken = default)
        where TRequest : IRequest<TResponse>;
}
