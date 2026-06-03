using System;
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
        GD.Print("- selected texture packs and material resources load");
        var texturePaths = new[]
        {
            "res://assets/runtime_textures/kenney_retro_textures_fantasy/floor_ground_grass.png",
            "res://assets/runtime_textures/kenney_retro_textures_fantasy/floor_wood_planks.png",
            "res://assets/runtime_textures/kenney_retro_textures_fantasy/floor_ground_water.png",
            "res://assets/runtime_textures/tree_pack/tree04.png",
            "res://assets/runtime_textures/quaternius_props/T_Trim_Props_BaseColor.png",
            "res://assets/runtime_textures/quaternius_outfits/T_Peasant_BaseColor.png",
            "res://assets/runtime_textures/quaternius_base_characters/T_Hair_1_BaseColor.png",
        };

        foreach (var path in texturePaths)
        {
            Check(System.IO.File.Exists(ProjectSettings.GlobalizePath(path)), $"texture file exists: {path.GetFile()}");
        }

        var material = ArtMaterialCatalog.Environment("#5a3a24", ArtMaterialKind.Wood);
        Check(material.AlbedoTexture is not null, "procedural wood material has an albedo texture");
        Check(ArtMaterialCatalog.Environment("#4f7538", ArtMaterialKind.GroundGrass).AlbedoTexture is not null, "procedural grass material has an albedo texture");
        Check(ArtMaterialCatalog.TreeLeaves("#2f6b36", 12).AlbedoTexture is not null, "tree pack leaf variant material has an albedo texture");
        Check(ArtMaterialCatalog.Create("#6a5acd", ArtMaterialKind.CharacterCloth).AlbedoTexture is not null, "procedural character cloth material has an albedo texture");
        Check(ArtMaterialCatalog.Create("#3a2a1a", ArtMaterialKind.CharacterHair).AlbedoTexture is not null, "procedural character hair material has an albedo texture");
        Check(GD.Load<StandardMaterial3D>("res://assets/materials/ground.tres") is not null, "ground fallback material resource still parses");
        Check(GD.Load<StandardMaterial3D>("res://assets/materials/player.tres") is not null, "player fallback material resource still parses");
    }
}
