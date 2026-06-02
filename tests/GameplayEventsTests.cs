using System.Collections.Generic;
using System.Threading.Tasks;
using DawnOfBlade.Communication;
using Xunit;

namespace DawnOfBlade.Tests;

/// <summary>
/// Confirms the game's domain events implement the communication contracts and flow through the
/// in-process bus with their fields intact, the way GameManager publishes them.
/// </summary>
public class GameplayEventsTests
{
    [Fact]
    public async Task ResourceGathered_FlowsToSubscriberWithFieldsIntact()
    {
        var bus = new InProcessCommunicationService();
        ResourceGathered? received = null;
        bus.Subscribe<ResourceGathered>((envelope, _) =>
        {
            received = envelope.Message;
            return ValueTask.CompletedTask;
        });

        await bus.PublishAsync(new ResourceGathered("logs", "woodcutting", 25));

        Assert.NotNull(received);
        Assert.Equal("logs", received!.ItemId);
        Assert.Equal("woodcutting", received.SkillId);
        Assert.Equal(25, received.Experience);
    }

    [Fact]
    public async Task SkillLeveledUp_AndEnemyDefeated_AreDeliveredInOrder()
    {
        var bus = new InProcessCommunicationService();
        var log = new List<string>();
        bus.Subscribe<SkillLeveledUp>((e, _) =>
        {
            log.Add($"level:{e.Message.SkillId}:{e.Message.Level}");
            return ValueTask.CompletedTask;
        });
        bus.Subscribe<EnemyDefeated>((e, _) =>
        {
            log.Add($"defeat:{e.Message.EnemyName}:{e.Message.CoinReward}");
            return ValueTask.CompletedTask;
        });

        await bus.PublishAsync(new SkillLeveledUp("strength", 5));
        await bus.PublishAsync(new EnemyDefeated("Training Dummy", 17));

        Assert.Equal(new[] { "level:strength:5", "defeat:Training Dummy:17" }, log);
    }
}
