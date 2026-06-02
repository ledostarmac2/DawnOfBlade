using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DawnOfBlade.Communication;

/// <summary>
/// Deterministic in-process communication service. Event handlers run in subscription order;
/// requests require one registered handler. Registration tokens remove handlers when disposed.
/// </summary>
public sealed class InProcessCommunicationService : ICommunicationService
{
    private readonly object _gate = new();
    private readonly Dictionary<Type, List<IEventSubscription>> _eventSubscriptions = new();
    private readonly Dictionary<Type, IRequestRegistration> _requestHandlers = new();

    public IDisposable Subscribe<TEvent>(
        Func<MessageEnvelope<TEvent>, CancellationToken, ValueTask> handler)
        where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        var subscription = new EventSubscription<TEvent>(this, handler);
        lock (_gate)
        {
            if (!_eventSubscriptions.TryGetValue(typeof(TEvent), out var subscriptions))
            {
                subscriptions = new List<IEventSubscription>();
                _eventSubscriptions.Add(typeof(TEvent), subscriptions);
            }

            subscriptions.Add(subscription);
        }

        return subscription;
    }

    public IDisposable RegisterHandler<TRequest, TResponse>(
        Func<MessageEnvelope<TRequest>, CancellationToken, ValueTask<TResponse>> handler)
        where TRequest : IRequest<TResponse>
    {
        ArgumentNullException.ThrowIfNull(handler);

        var registration = new RequestRegistration<TRequest, TResponse>(this, handler);
        lock (_gate)
        {
            if (!_requestHandlers.TryAdd(typeof(TRequest), registration))
            {
                throw new InvalidOperationException(
                    $"A request handler is already registered for {typeof(TRequest).FullName}.");
            }
        }

        return registration;
    }

    public ValueTask PublishAsync<TEvent>(
        TEvent message,
        Guid? correlationId = null,
        Guid? causationId = null,
        CancellationToken cancellationToken = default)
        where TEvent : IEvent =>
        PublishAsync(
            MessageEnvelope<TEvent>.Create(message, correlationId, causationId),
            cancellationToken);

    public async ValueTask PublishAsync<TEvent>(
        MessageEnvelope<TEvent> envelope,
        CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(envelope);
        cancellationToken.ThrowIfCancellationRequested();

        IEventSubscription[] subscriptions;
        lock (_gate)
        {
            subscriptions = _eventSubscriptions.TryGetValue(typeof(TEvent), out var registered)
                ? registered.ToArray()
                : Array.Empty<IEventSubscription>();
        }

        foreach (var subscription in subscriptions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await subscription.DispatchAsync(envelope, cancellationToken);
        }
    }

    public ValueTask<TResponse> SendAsync<TRequest, TResponse>(
        TRequest message,
        Guid? correlationId = null,
        Guid? causationId = null,
        CancellationToken cancellationToken = default)
        where TRequest : IRequest<TResponse> =>
        SendAsync<TRequest, TResponse>(
            MessageEnvelope<TRequest>.Create(message, correlationId, causationId),
            cancellationToken);

    public ValueTask<TResponse> SendAsync<TRequest, TResponse>(
        MessageEnvelope<TRequest> envelope,
        CancellationToken cancellationToken = default)
        where TRequest : IRequest<TResponse>
    {
        ArgumentNullException.ThrowIfNull(envelope);
        cancellationToken.ThrowIfCancellationRequested();

        IRequestRegistration registration;
        lock (_gate)
        {
            if (!_requestHandlers.TryGetValue(typeof(TRequest), out registration!))
            {
                throw new InvalidOperationException(
                    $"No request handler is registered for {typeof(TRequest).FullName}.");
            }
        }

        if (registration is not RequestRegistration<TRequest, TResponse> typedRegistration)
        {
            throw new InvalidOperationException(
                $"The registered response type for {typeof(TRequest).FullName} does not match {typeof(TResponse).FullName}.");
        }

        return typedRegistration.DispatchAsync(envelope, cancellationToken);
    }

    private void Remove(IEventSubscription subscription)
    {
        lock (_gate)
        {
            if (!_eventSubscriptions.TryGetValue(subscription.MessageType, out var subscriptions))
            {
                return;
            }

            subscriptions.Remove(subscription);
            if (subscriptions.Count == 0)
            {
                _eventSubscriptions.Remove(subscription.MessageType);
            }
        }
    }

    private void Remove(IRequestRegistration registration)
    {
        lock (_gate)
        {
            if (_requestHandlers.TryGetValue(registration.MessageType, out var current) &&
                ReferenceEquals(current, registration))
            {
                _requestHandlers.Remove(registration.MessageType);
            }
        }
    }

    private interface IEventSubscription
    {
        Type MessageType { get; }
        ValueTask DispatchAsync(object envelope, CancellationToken cancellationToken);
    }

    private interface IRequestRegistration
    {
        Type MessageType { get; }
    }

    private sealed class EventSubscription<TEvent> : IEventSubscription, IDisposable
        where TEvent : IEvent
    {
        private readonly InProcessCommunicationService _owner;
        private readonly Func<MessageEnvelope<TEvent>, CancellationToken, ValueTask> _handler;
        private int _isDisposed;

        public EventSubscription(
            InProcessCommunicationService owner,
            Func<MessageEnvelope<TEvent>, CancellationToken, ValueTask> handler)
        {
            _owner = owner;
            _handler = handler;
        }

        public Type MessageType => typeof(TEvent);

        public ValueTask DispatchAsync(object envelope, CancellationToken cancellationToken) =>
            _handler((MessageEnvelope<TEvent>)envelope, cancellationToken);

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
            {
                return;
            }

            _owner.Remove(this);
        }
    }

    private sealed class RequestRegistration<TRequest, TResponse> : IRequestRegistration, IDisposable
        where TRequest : IRequest<TResponse>
    {
        private readonly InProcessCommunicationService _owner;
        private readonly Func<MessageEnvelope<TRequest>, CancellationToken, ValueTask<TResponse>> _handler;
        private int _isDisposed;

        public RequestRegistration(
            InProcessCommunicationService owner,
            Func<MessageEnvelope<TRequest>, CancellationToken, ValueTask<TResponse>> handler)
        {
            _owner = owner;
            _handler = handler;
        }

        public Type MessageType => typeof(TRequest);

        public ValueTask<TResponse> DispatchAsync(
            MessageEnvelope<TRequest> envelope,
            CancellationToken cancellationToken) =>
            _handler(envelope, cancellationToken);

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
            {
                return;
            }

            _owner.Remove(this);
        }
    }
}
