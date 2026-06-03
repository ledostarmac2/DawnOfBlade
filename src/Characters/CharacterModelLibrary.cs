using System.Collections.Generic;
using Godot;

namespace DawnOfBlade.Characters;

/// <summary>
/// Loads the real rigged character models shipped under res://assets/runtime_models/characters
/// (Quaternius Modular Character Outfits — peasant and ranger, male and female) and instantiates one
/// deterministically per NPC seed. The export bundles that tree; when a model is unavailable the caller
/// falls back to the procedural <see cref="HumanoidVisual"/>, so the world is never left without bodies.
/// </summary>
public static class CharacterModelLibrary
{
    private const string Root = "res://assets/runtime_models/characters/";

    private static readonly string[] MasculineModels = { Root + "Male_Peasant.gltf", Root + "Male_Ranger.gltf" };
    private static readonly string[] FeminineModels = { Root + "Female_Peasant.gltf", Root + "Female_Ranger.gltf" };

    private static readonly Dictionary<string, PackedScene?> Cache = new();

    /// <summary>The pose-bind height of the bundled models, so callers can match the collision capsule.</summary>
    public const float ModelFootOffset = -0.9f;

    public static IReadOnlyList<string> AllModelPaths()
    {
        var all = new List<string>();
        all.AddRange(MasculineModels);
        all.AddRange(FeminineModels);
        return all;
    }

    /// <summary>Picks the model variant for an NPC: sex by presentation, outfit by seed.</summary>
    public static string ModelPathFor(Appearance appearance, int seed)
    {
        var set = appearance.Presentation == "feminine" ? FeminineModels : MasculineModels;
        return set[Mathf.Abs(seed) % set.Length];
    }

    /// <summary>
    /// Returns a ready-to-parent visual built from a real character model, or null if the model
    /// could not be loaded (caller should then use the procedural humanoid).
    /// </summary>
    public static Node3D? TryInstantiate(Appearance appearance, int seed)
    {
        var scene = Load(ModelPathFor(appearance, seed));
        if (scene is null)
        {
            return null;
        }

        var model = scene.Instantiate<Node3D>();
        if (model is null)
        {
            return null;
        }

        // Wrap so the imported skeleton keeps its own transform; rotate to face Godot-forward (-Z).
        var root = new Node3D { Name = "Humanoid", Position = new Vector3(0, ModelFootOffset, 0) };
        root.RotationDegrees = new Vector3(0, 180, 0);
        root.AddChild(model);
        return root;
    }

    private static PackedScene? Load(string path)
    {
        if (Cache.TryGetValue(path, out var cached))
        {
            return cached;
        }

        var scene = ResourceLoader.Exists(path) ? ResourceLoader.Load<PackedScene>(path) : null;
        Cache[path] = scene;
        return scene;
    }
}
