using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DawnOfBlade.Communication;
using Xunit;

namespace DawnOfBlade.Tests;

public class CommunicationServiceTests
{
    [Fact]
    public async Task PublishAsync_NotifiesSubscribersInRegistrationOrder()
    {
        var service = new InProcessCommunicationService();
        var received = new List<string>();
        service.Subscribe<PlayerMoved>((envelope, _) =>
        {
            received.Add($"first:{envelope.Message.PlayerId}");
            return ValueTask.CompletedTask;
        });
        service.Subscribe<PlayerMoved>((envelope, _) =>
        {
            received.Add($"second:{envelope.Message.PlayerId}");
            return ValueTask.CompletedTask;
        });

        await service.PublishAsync(new PlayerMoved("mira", 4, 7));

        Assert.Equal(new[] { "first:mira", "second:mira" }, received);
    }

    [Fact]
    public async Task Subscription_DisposeStopsFutureNotifications()
    {
        var service = new InProcessCommunicationService();
        var notifications = 0;
        var subscription = service.Subscribe<PlayerMoved>((_, _) =>
        {
            notifications++;
            return ValueTask.CompletedTask;
        });

        await service.PublishAsync(new PlayerMoved("mira", 4, 7));
        subscription.Dispose();
        await service.PublishAsync(new PlayerMoved("mira", 5, 8));

        Assert.Equal(1, notifications);
    }

    [Fact]
    public async Task SendAsync_ReturnsRegisteredHandlerResponseAndMetadata()
    {
        var service = new InProcessCommunicationService();
        var correlationId = Guid.NewGuid();
        MessageEnvelope<CountInventory>? received = null;
        service.RegisterHandler<CountInventory, int>((envelope, _) =>
        {
            received = envelope;
            return ValueTask.FromResult(3);
        });

        var count = await service.SendAsync<CountInventory, int>(
            new CountInventory("sunleaf"),
            correlationId);

        Assert.Equal(3, count);
        Assert.NotEqual(Guid.Empty, received!.MessageId);
        Assert.Equal(correlationId, received.CorrelationId);
        Assert.Equal("sunleaf", received.Message.ItemId);
    }

    [Fact]
    public async Task SendAsync_ThrowsWhenNoHandlerIsRegistered()
    {
        var service = new InProcessCommunicationService();

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.SendAsync<CountInventory, int>(new CountInventory("sunleaf")));
    }

    [Fact]
    public void RegisterHandler_RejectsDuplicateRequestHandlers()
    {
        var service = new InProcessCommunicationService();
        service.RegisterHandler<CountInventory, int>((_, _) => ValueTask.FromResult(1));

        Assert.Throws<InvalidOperationException>(
            () => service.RegisterHandler<CountInventory, int>((_, _) => ValueTask.FromResult(2)));
    }

    private sealed record PlayerMoved(string PlayerId, int X, int Y) : IEvent;
    private sealed record CountInventory(string ItemId) : IRequest<int>;
}
