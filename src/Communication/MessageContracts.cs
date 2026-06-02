using System;

namespace DawnOfBlade.Communication;

/// <summary>
/// Marker for messages that cross a communication service boundary.
/// Keep implementations immutable so a future remote adapter can serialize them safely.
/// </summary>
public interface IMessage
{
}

/// <summary>
/// Notification that may be observed by zero or more subscribers.
/// </summary>
public interface IEvent : IMessage
{
}

/// <summary>
/// Request that is handled by exactly one registered handler.
/// </summary>
public interface IRequest<TResponse> : IMessage
{
}

/// <summary>
/// Transport-neutral metadata carried with every message.
/// </summary>
public sealed record MessageEnvelope<TMessage>(
    Guid MessageId,
    TMessage Message,
    DateTimeOffset CreatedAt,
    Guid? CorrelationId = null,
    Guid? CausationId = null)
    where TMessage : IMessage
{
    public static MessageEnvelope<TMessage> Create(
        TMessage message,
        Guid? correlationId = null,
        Guid? causationId = null,
        Guid? messageId = null,
        DateTimeOffset? createdAt = null)
    {
        ArgumentNullException.ThrowIfNull(message);

        return new MessageEnvelope<TMessage>(
            messageId ?? Guid.NewGuid(),
            message,
            createdAt ?? DateTimeOffset.UtcNow,
            correlationId,
            causationId);
    }
}
