using DawnOfBlade.Inventory;
using DawnOfBlade.World;
using Xunit;

namespace DawnOfBlade.Tests;

public class MmorpgLifecycleTests
{
    [Fact]
    public void BankStorage_DepositAndWithdrawMoveItemsWithoutDuplication()
    {
        var inventory = new Inventory.Inventory();
        var bank = new BankStorage();
        inventory.Add("logs", 3);

        Assert.True(bank.Deposit(inventory, "logs", 2));
        Assert.Equal(1, inventory.Count("logs"));
        Assert.Equal(2, bank.Count("logs"));

        Assert.True(bank.Withdraw(inventory, "logs"));
        Assert.Equal(2, inventory.Count("logs"));
        Assert.Equal(1, bank.Count("logs"));
    }

    [Fact]
    public void BankStorage_RejectsUnavailableWithdrawals()
    {
        var inventory = new Inventory.Inventory();
        var bank = new BankStorage();

        Assert.False(bank.Withdraw(inventory, "copper_ore"));
        Assert.Empty(inventory.Items);
    }

    [Fact]
    public void ResourceRespawnState_RespawnsAtConfiguredHeartbeat()
    {
        var state = new ResourceRespawnState();

        state.Deplete(10, 3);
        Assert.True(state.IsDepleted);
        Assert.Equal(13, state.RespawnsAtTick);

        state.AdvanceTick(12);
        Assert.True(state.IsDepleted);

        state.AdvanceTick(13);
        Assert.False(state.IsDepleted);
    }
}
