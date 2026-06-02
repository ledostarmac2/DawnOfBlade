using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DawnOfBlade.Combat;
using DawnOfBlade.Communication;
using DawnOfBlade.GameSystems;
using DawnOfBlade.World.Grid;
using Xunit;

namespace DawnOfBlade.Tests;

public class GameSystemsTests
{
    /// <summary>Deterministic IRandomSource that replays scripted integer/double rolls.</summary>
    private sealed class ScriptedRandom : IRandomSource
    {
        private readonly Queue<int> _ints;
        private readonly Queue<double> _doubles;

        public ScriptedRandom(int[]? ints = null, double[]? doubles = null)
        {
            _ints = new Queue<int>(ints ?? Array.Empty<int>());
            _doubles = new Queue<double>(doubles ?? Array.Empty<double>());
        }

        public double NextDouble() => _doubles.Dequeue();
        public int Next(int maxExclusive) => _ints.Dequeue();
        public int Next(int minInclusive, int maxExclusive) => _ints.Dequeue();
    }

    // ---- Part 18: loot ----------------------------------------------------

    [Fact]
    public void LootRoller_AlwaysEmitsGuaranteedDrops_AndOneWeightedStandard()
    {
        var table = new LootTable(
            guaranteed: new[] { new LootDrop(10, 1) },                             // bones
            standard: new[] { new WeightedDrop(20, 1, 3000), new WeightedDrop(21, 1, 2000) },
            rare: new[] { new RareDrop(99, 1, 128) });

        // standard roll 2500 -> first entry (cumulative 3000 >= 2500); rare roll 0 -> drops.
        var drops = LootRoller.Roll(table, new ScriptedRandom(ints: new[] { 2500, 0 }));

        Assert.Contains(drops, d => d.ItemId == 10);  // guaranteed
        Assert.Contains(drops, d => d.ItemId == 20);  // standard pick
        Assert.DoesNotContain(drops, d => d.ItemId == 21);
        Assert.Contains(drops, d => d.ItemId == 99);  // rare hit
    }

    [Fact]
    public void LootRoller_RollBeyondWeightPool_DropsNoStandardItem_AndRareCanMiss()
    {
        var table = new LootTable(
            standard: new[] { new WeightedDrop(20, 1, 3000), new WeightedDrop(21, 1, 2000) }, // total 5000
            rare: new[] { new RareDrop(99, 1, 128) });

        // roll 8000 > 5000 -> nothing standard; rare roll 5 != 0 -> miss.
        var drops = LootRoller.Roll(table, new ScriptedRandom(ints: new[] { 8000, 5 }));

        Assert.Empty(drops);
    }

    // ---- Part 18: spawner pool -------------------------------------------

    [Fact]
    public void ResourceSpawnerPool_DepletesThenRespawnsOnSchedule()
    {
        var ore = new GridCoordinate(4, 7);
        var pool = new ResourceSpawnerPool(new[] { ore, new GridCoordinate(8, 1) });
        Assert.Equal(2, pool.ActiveCount);

        pool.Deplete(ore, currentTick: 100, respawnDelayTicks: 50);
        Assert.False(pool.IsActive(ore));
        Assert.Equal(1, pool.ActiveCount);

        Assert.Empty(pool.Tick(120));            // before timestamp 150
        Assert.False(pool.IsActive(ore));

        var respawned = pool.Tick(150);          // at timestamp
        Assert.Equal(new[] { ore }, respawned);
        Assert.True(pool.IsActive(ore));
        Assert.Equal(2, pool.ActiveCount);
    }

    // ---- Part 19: market engine ------------------------------------------

    [Fact]
    public void Market_BuyCrossesRestingSell_AtSellPrice_WithSpreadRefund()
    {
        var market = new MarketEngine();
        market.PlaceSellOrder("seller", itemId: 30, quantity: 5, unitPrice: 10);

        var result = market.PlaceBuyOrder("buyer", itemId: 30, quantity: 5, maxUnitPrice: 12, buyerGold: 60);

        Assert.True(result.Accepted);
        Assert.Equal(5, result.FilledQuantity);
        Assert.Equal(5, market.DepotItemCount("buyer", 30));
        Assert.Equal(50, market.DepotGold("seller"));     // 5 * 10 (resting price)
        Assert.Equal(10, market.DepotGold("buyer"));      // spread refund: 5 * (12 - 10)
        Assert.Equal(0, market.EscrowGold("buyer"));
        Assert.Equal(0, market.OpenSellCount);
    }

    [Fact]
    public void Market_BuyWithoutMatch_RestsAndEscrowsGold()
    {
        var market = new MarketEngine();

        var result = market.PlaceBuyOrder("buyer", itemId: 30, quantity: 3, maxUnitPrice: 5, buyerGold: 100);

        Assert.True(result.Accepted);
        Assert.Equal(0, result.FilledQuantity);
        Assert.Equal(3, result.RestingQuantity);
        Assert.Equal(1, market.OpenBuyCount);
        Assert.Equal(15, market.EscrowGold("buyer"));     // 3 * 5 locked
    }

    [Fact]
    public void Market_RejectsBuyWhenGoldCannotCoverOrder()
    {
        var market = new MarketEngine();

        var result = market.PlaceBuyOrder("buyer", itemId: 30, quantity: 3, maxUnitPrice: 5, buyerGold: 10);

        Assert.False(result.Accepted);
        Assert.Equal(0, market.OpenBuyCount);
        Assert.Equal(0, market.EscrowGold("buyer"));
    }

    [Fact]
    public void Market_PartialFill_AcrossAscendingSellPrices()
    {
        var market = new MarketEngine();
        market.PlaceSellOrder("s1", itemId: 30, quantity: 5, unitPrice: 10);
        market.PlaceSellOrder("s2", itemId: 30, quantity: 5, unitPrice: 12);

        var result = market.PlaceBuyOrder("buyer", itemId: 30, quantity: 8, maxUnitPrice: 12, buyerGold: 96);

        Assert.Equal(8, result.FilledQuantity);
        Assert.Equal(8, market.DepotItemCount("buyer", 30));
        Assert.Equal(50, market.DepotGold("s1"));   // 5 @ 10
        Assert.Equal(36, market.DepotGold("s2"));   // 3 @ 12
        Assert.Equal(10, market.DepotGold("buyer")); // refund from the 5 @ 10 leg only
        Assert.Equal(1, market.OpenSellCount);      // s2 has 2 left
    }

    [Fact]
    public void Market_LogsTransactionsToAuditBus()
    {
        var log = new TransactionLogger();
        var market = new MarketEngine(log);
        market.PlaceSellOrder("seller", 30, 2, 10);
        market.PlaceBuyOrder("buyer", 30, 2, 10, buyerGold: 20);

        Assert.True(log.Count >= 1);
        Assert.Contains(log.Records, r =>
            r.Action == TransactionAction.MarketBuy && r.ItemId == 30 && r.QuantityChanged == 2 &&
            r.ActorId == "buyer" && r.TargetId == "seller");
    }

    // ---- Part 19: transaction logger + bus -------------------------------

    [Fact]
    public async Task TransactionLogger_RecordsAndPublishesEachPacket()
    {
        var bus = new InProcessCommunicationService();
        var seen = new List<TransactionRecord>();
        bus.Subscribe<TransactionLogged>((envelope, _) =>
        {
            seen.Add(envelope.Message.Record);
            return ValueTask.CompletedTask;
        });

        var log = new TransactionLogger(bus);
        log.Log(new TransactionRecord(
            Timestamp: 42, ActorId: "p1", TargetId: null, Action: TransactionAction.BankDeposit,
            ItemId: 5, QuantityChanged: 3, SourceContainerId: "inv:p1", DestinationContainerId: "bank:p1"));

        await Task.CompletedTask;
        Assert.Equal(1, log.Count);
        Assert.Single(seen);
        Assert.Equal(TransactionAction.BankDeposit, seen[0].Action);
        Assert.Equal("bank:p1", seen[0].DestinationContainerId);
    }

    // ---- Part 20: fresh spawn + disconnect policy ------------------------

    [Fact]
    public void CharacterInitializer_SeedsSafeZoneSpawn_StarterKit_AndZeroedSkills()
    {
        var skillIds = new[] { "attack", "woodcutting", "mining", "language" };
        var fresh = CharacterInitializer.Create("char-1", "acct-1", skillIds);

        Assert.Equal(CharacterInitializer.VerdantValleyCenter, fresh.Player.Coordinates);
        Assert.Equal(0, fresh.Player.WalletGold);
        Assert.Equal(CharacterInitializer.StartingHealth, fresh.Player.CurrentHealth);

        // Starter kit lands in slots 0..2.
        Assert.Equal(3, fresh.Inventory.Count);
        Assert.Equal(0, fresh.Inventory[0].SlotIndex);
        Assert.Equal(CharacterInitializer.BreadItemId, fresh.Inventory[2].ItemId);
        Assert.Equal(10, fresh.Inventory[2].Quantity);

        Assert.Equal(skillIds.Length, fresh.Skills.Count);
        Assert.All(fresh.Skills, s => Assert.Equal(0.0, s.ExperiencePoints));
    }

    [Fact]
    public void DisconnectPolicy_SafeZone_AlwaysCleanLogout()
    {
        var decision = DisconnectPolicy.Evaluate(CombatStatus.CombatEngaged, WorldZone.VerdantValley);
        Assert.True(decision.SaveImmediately);
        Assert.False(decision.RemainsAsDummy);
    }

    [Fact]
    public void DisconnectPolicy_CombatEngaged_OutsideSafeZone_LocksAsDummy()
    {
        var decision = DisconnectPolicy.Evaluate(CombatStatus.CombatEngaged, WorldZone.SunscorchedDunes);
        Assert.False(decision.SaveImmediately);
        Assert.Equal(DisconnectPolicy.CombatLogoutLockTicks, decision.GridLockTicks);
    }

    [Fact]
    public void DisconnectPolicy_NeutralInWilderness_LocksAsDummy_ButNeutralInContestedIsClean()
    {
        var wilderness = DisconnectPolicy.Evaluate(CombatStatus.Neutral, WorldZone.WhisperingMire);
        Assert.True(wilderness.RemainsAsDummy);
        Assert.Equal(60, wilderness.GridLockTicks);

        var contested = DisconnectPolicy.Evaluate(CombatStatus.Neutral, WorldZone.SunscorchedDunes);
        Assert.True(contested.SaveImmediately);
        Assert.False(contested.RemainsAsDummy);
    }

    // ---- Part 17: persistence models -------------------------------------

    [Fact]
    public void PlayerRow_Coordinates_RoundTripThroughText()
    {
        var row = new PlayerRow("c", "a", new GridCoordinate(-12, 34), 10, 100, 5_000_000_000L);
        Assert.Equal("-12,34", row.CoordinatesText);
        Assert.Equal(new GridCoordinate(-12, 34), PlayerRow.ParseCoordinates(row.CoordinatesText));
        Assert.Equal(5_000_000_000L, row.WalletGold); // 64-bit gold survives
    }

    [Fact]
    public void RecipeData_MapsIngredientsToOutput()
    {
        var bronze = new RecipeData(
            OutputItemId: 300, OutputQuantity: 1, RequiredSkillLevel: 1,
            Ingredients: new Dictionary<int, int> { [100] = 1, [101] = 1 });

        Assert.Equal(2, bronze.Ingredients.Count);
        Assert.Equal(1, bronze.Ingredients[100]);
        Assert.Equal(300, bronze.OutputItemId);
    }
}
