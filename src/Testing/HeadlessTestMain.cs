using System;
using System.Linq;
using DawnOfBlade.Combat;
using DawnOfBlade.Engine.Ai;
using DawnOfBlade.Engine.Progression;
using DawnOfBlade.Engine.Spatial;
using DawnOfBlade.Interaction;
using DawnOfBlade.World;
using Godot;

namespace DawnOfBlade.Testing;

/// <summary>
/// A headless, in-engine test runner. Unlike the pure xUnit suite (which never boots Godot), this
/// instantiates the <em>actual</em> Godot nodes — <see cref="HostileActor"/>, <see cref="PrototypeNpc"/>,
/// <see cref="ActorAiAgent"/> — inside a live SceneTree and asserts that AI behavior moves their real
/// <c>Node3D</c> transforms. Run it with <c>tools/run-godot-tests.ps1</c>; it prints PASS/FAIL lines
/// and quits with an exit code equal to the failure count (0 = success).
/// </summary>
public partial class HeadlessTestMain : Node
{
    private int _checks;
    private int _failures;

    public override void _Ready()
    {
        GD.Print("== Dawn of Blade headless engine tests ==");
        try
        {
            TestHostileChasesAndMovesInScene();
            TestHostileLeashesHomeInScene();
            TestPassiveNpcWandersButNeverChases();
            TestArtTextureResourcesLoad();
        }
        catch (Exception e)
        {
            GD.PrintErr($"[FATAL] {e}");
            _failures++;
        }

        GD.Print($"== {_checks - _failures}/{_checks} checks passed ==");
        GD.Print(_failures == 0 ? "RESULT: ALL GODOT TESTS PASSED" : $"RESULT: {_failures} GODOT CHECK(S) FAILED");
        GetTree().Quit(_failures);
    }

    private void Check(bool condition, string name)
    {
        _checks++;
        if (condition)
        {
            GD.Print($"  [PASS] {name}");
        }
        else
        {
            _failures++;
            GD.PrintErr($"  [FAIL] {name}");
        }
    }

    private void TestHostileChasesAndMovesInScene()
    {
        GD.Print("- aggressive monster chases a weaker target and its transform moves");
        var grid = new CollisionGrid(48, 48);
        var anchor = new TrueTile(5, 5);

        var actor = new HostileActor
        {
            Archetype = "Aggressive",
            AttackLevel = 8, StrengthLevel = 8, DefenseLevel = 7, MaxHitpoints = 13,
            WanderRadius = 3, AggroRadius = 6, LeashRadius = 10,
        };
        AddChild(actor); // runs _Ready -> ResetStats on the real node

        Check(actor.CombatLevel == CombatLevel.Compute(8, 8, 7, 13), "combat level derived on the real node");

        var agent = new ActorAiAgent();
        AddChild(agent);
        agent.Configure(actor, actor.BuildBrain(anchor, new SystemRandomSource(1)), grid, tileSizeMeters: 2.0f);

        var startX = actor.GlobalPosition.X;
        var target = new TrueTile(10, 5);
        var reached = false;
        for (var tick = 0; tick < 24 && !reached; tick++)
        {
            reached = agent.Tick(Perception.Of(target, combatLevel: 3)).InAttackRange;
        }

        Check(reached, "monster reached attack range");
        Check(actor.GlobalPosition.X > startX + 1.0f, "node3D transform actually advanced toward the target");
        Check(agent.Tile.ManhattanDistance(target) <= 1, "monster ended adjacent to (not on top of) the target");

        actor.QueueFree();
        agent.QueueFree();
    }

    private void TestHostileLeashesHomeInScene()
    {
        GD.Print("- aggressive monster leashes and returns its transform home");
        var grid = new CollisionGrid(64, 64);
        var anchor = new TrueTile(8, 8);

        var actor = new HostileActor
        {
            Archetype = "Aggressive",
            AttackLevel = 40, StrengthLevel = 40, DefenseLevel = 40, MaxHitpoints = 40, // high level: only the leash ends the chase
            WanderRadius = 2, AggroRadius = 6, LeashRadius = 5,
        };
        AddChild(actor);

        var agent = new ActorAiAgent();
        AddChild(agent);
        agent.Configure(actor, actor.BuildBrain(anchor, new SystemRandomSource(2)), grid, tileSizeMeters: 1.0f);
        var homeWorld = actor.GlobalPosition;

        var overshot = false;
        for (var tick = 0; tick < 40 && !overshot; tick++)
        {
            var bait = new TrueTile(agent.Tile.X + 2, 8);
            agent.Tick(Perception.Of(bait, combatLevel: 5));
            overshot = anchor.ChebyshevDistance(agent.Tile) > 5;
        }

        Check(overshot, "monster was lured past its leash");

        var home = false;
        for (var tick = 0; tick < 80 && !home; tick++)
        {
            home = agent.Tick(Perception.None).Position == anchor;
        }

        Check(home, "monster walked all the way back to its anchor tile");
        Check(actor.GlobalPosition.DistanceTo(homeWorld) < 0.001f, "node3D transform returned to its spawn position");

        actor.QueueFree();
        agent.QueueFree();
    }

    private void TestPassiveNpcWandersButNeverChases()
    {
        GD.Print("- passive villager wanders its area but never chases");
        var grid = new CollisionGrid(48, 48);
        var anchor = new TrueTile(20, 20);

        var npc = new PrototypeNpc { Seed = 2, WanderRadius = 3 };
        AddChild(npc); // runs _Ready -> randomizer + (no-op) tint

        var agent = new ActorAiAgent();
        AddChild(agent);
        agent.Configure(npc, npc.BuildBrain(anchor, new SystemRandomSource(7)), grid, tileSizeMeters: 1.0f);

        var adjacentTarget = Perception.Of(new TrueTile(20, 21), combatLevel: 1);
        var everChased = false;
        var stayedInside = true;
        var moved = false;
        for (var tick = 0; tick < 240; tick++)
        {
            var step = agent.Tick(adjacentTarget);
            everChased |= step.State == AiState.Chasing;
            stayedInside &= anchor.ChebyshevDistance(step.Position) <= 3;
            moved |= step.Moved;
        }

        Check(!everChased, "passive villager never entered a chase, even with an adjacent target");
        Check(stayedInside, "passive villager stayed within its wander area");
        Check(moved, "passive villager actually strolled around");

        npc.QueueFree();
        agent.QueueFree();
    }

    private void TestArtTextureResourcesLoad()
    {
        GD.Print("- shipped texture set is broad and every catalog texture is used");

        // Every texture the catalog can route to must actually ship under res://assets/runtime_textures
        // (the export bundles that tree; the raw res://textures tree is excluded), so the built .exe
        // renders them instead of falling back to flat colour.
        var allPaths = ArtMaterialCatalog.AllRuntimeTexturePaths();
        var missing = allPaths.Where(path => !System.IO.File.Exists(ProjectSettings.GlobalizePath(path))).ToList();
        Check(missing.Count == 0, $"all {allPaths.Count} catalog textures exist on disk" + (missing.Count == 0 ? "" : $" (missing: {string.Join(", ", missing.Take(5).Select(p => p.GetFile()))})"));
        Check(allPaths.Count >= 180, $"catalog routes to a broad shipped texture set ({allPaths.Count} files)");

        // Drive every palette entry so each environment/character texture is exercised, not just shipped.
        var environmentKinds = new[]
        {
            ArtMaterialKind.GroundGrass, ArtMaterialKind.GroundDirt, ArtMaterialKind.Stone, ArtMaterialKind.Wood,
            ArtMaterialKind.Roof, ArtMaterialKind.Water, ArtMaterialKind.Metal, ArtMaterialKind.Furniture,
            ArtMaterialKind.Props, ArtMaterialKind.Leaves,
        };
        foreach (var kind in environmentKinds)
        {
            var resolved = true;
            for (var variant = 0; variant < 64; variant++)
            {
                resolved &= ArtMaterialCatalog.Environment("#808078", kind, variant).AlbedoTexture is not null;
            }

            Check(resolved, $"every {kind} palette variant resolves to an albedo texture");
        }

        var characterKinds = new[] { ArtMaterialKind.CharacterCloth, ArtMaterialKind.CharacterSkin, ArtMaterialKind.CharacterHair };
        foreach (var kind in characterKinds)
        {
            var resolved = true;
            for (var variant = 0; variant < 16; variant++)
            {
                resolved &= ArtMaterialCatalog.Create("#9a8d7a", kind, variant).AlbedoTexture is not null;
            }

            Check(resolved, $"every {kind} palette variant resolves to an albedo texture");
        }

        var treeOk = true;
        for (var variant = 1; variant <= 36; variant++)
        {
            treeOk &= ArtMaterialCatalog.TreeLeaves("#2f6b36", variant).AlbedoTexture is not null;
        }

        Check(treeOk, "all 36 tree-pack leaf variants resolve to an albedo texture");

        var bushOk = true;
        for (var variant = 1; variant <= 8; variant++)
        {
            bushOk &= ArtMaterialCatalog.BushLeaves("#3f7a3a", variant).AlbedoTexture is not null;
        }

        Check(bushOk, "all 8 tree-pack bush variants resolve to an albedo texture");

        Check(GD.Load<StandardMaterial3D>("res://assets/materials/ground.tres") is not null, "ground fallback material resource still parses");
        Check(GD.Load<StandardMaterial3D>("res://assets/materials/player.tres") is not null, "player fallback material resource still parses");
    }
}
