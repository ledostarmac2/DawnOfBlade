using System.Collections.Generic;
using System.Linq;
using Godot;

namespace DawnOfBlade.World;

public enum ArtMaterialKind
{
    Auto,
    CharacterCloth,
    CharacterSkin,
    CharacterHair,
    GroundGrass,
    GroundDirt,
    Stone,
    Wood,
    Roof,
    Water,
    Leaves,
    Bark,
    Metal,
    Furniture,
    Props,
}

/// <summary>
/// Central map from procedural material intent to the curated texture packs that actually ship under
/// res://assets/runtime_textures (the export bundles this tree; the raw res://textures tree is excluded).
/// Each kind owns a palette of real albedo files; a stable per-color (or explicit variant) index spreads
/// the whole palette across the world so almost every shipped texture is exercised in some form.
/// </summary>
public static class ArtMaterialCatalog
{
    private const string Retro = "res://assets/runtime_textures/kenney_retro_textures_fantasy/";
    private const string Kit = "res://assets/runtime_textures/kenney_retro_fantasy_kit/";
    private const string Hex = "res://assets/runtime_textures/kaykit_hexagon/";
    private const string Props = "res://assets/runtime_textures/quaternius_props/";
    private const string Outfits = "res://assets/runtime_textures/quaternius_outfits/";
    private const string BaseCharacters = "res://assets/runtime_textures/quaternius_base_characters/";
    private const string TreePack = "res://assets/runtime_textures/tree_pack/";

    private static readonly Dictionary<string, Texture2D?> TextureCache = new();
    private static readonly Dictionary<ArtMaterialKind, string[]> Palettes = BuildPalettes();

    public static StandardMaterial3D Create(string color, ArtMaterialKind kind = ArtMaterialKind.Auto, int variant = -1)
    {
        var resolved = kind == ArtMaterialKind.Auto ? ResolveCharacter(new Color(color)) : kind;
        return CreateResolved(color, resolved, variant);
    }

    public static StandardMaterial3D Environment(string color, ArtMaterialKind kind = ArtMaterialKind.Auto, int variant = -1)
    {
        var resolved = kind == ArtMaterialKind.Auto ? ResolveEnvironment(new Color(color)) : kind;
        return CreateResolved(color, resolved, variant);
    }

    public static StandardMaterial3D Water(Color tint)
    {
        var material = Create(tint.ToHtml(), ArtMaterialKind.Water);
        material.AlbedoColor = tint;
        material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        material.Roughness = 0.08f;
        return material;
    }

    public static StandardMaterial3D TreeLeaves(string color, int variant)
    {
        var clamped = Mathf.Clamp(variant, 1, 36);
        return CreateResolved(color, ArtMaterialKind.Leaves, -1, TreePack + $"tree{clamped:00}.png");
    }

    public static StandardMaterial3D BushLeaves(string color, int variant)
    {
        var clamped = Mathf.Clamp(variant, 1, 8);
        return CreateResolved(color, ArtMaterialKind.Leaves, -1, TreePack + $"bush{clamped:00}.png");
    }

    /// <summary>Every texture the catalog can route to, used for shipped-texture coverage verification.</summary>
    public static IReadOnlyList<string> AllRuntimeTexturePaths()
    {
        var set = new SortedSet<string>();
        foreach (var palette in Palettes.Values)
        {
            foreach (var path in palette)
            {
                set.Add(path);
            }
        }

        for (var i = 1; i <= 36; i++)
        {
            set.Add(TreePack + $"tree{i:00}.png");
        }

        for (var i = 1; i <= 8; i++)
        {
            set.Add(TreePack + $"bush{i:00}.png");
        }

        return set.ToList();
    }

    private static StandardMaterial3D CreateResolved(string color, ArtMaterialKind resolved, int variant, string? explicitPath = null)
    {
        var material = new StandardMaterial3D
        {
            AlbedoColor = new Color(color),
            Roughness = Roughness(resolved),
            Metallic = resolved == ArtMaterialKind.Metal ? 0.38f : 0.0f,
            SpecularMode = BaseMaterial3D.SpecularModeEnum.SchlickGgx,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
            Uv1Scale = UvScale(resolved),
        };

        if (resolved == ArtMaterialKind.Water)
        {
            material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        }

        var texture = LoadTexture(explicitPath ?? PickPath(resolved, color, variant));
        if (texture is not null)
        {
            material.AlbedoTexture = texture;
        }

        return material;
    }

    private static string PickPath(ArtMaterialKind kind, string color, int variant)
    {
        var palette = Palettes.TryGetValue(kind, out var found) && found.Length > 0
            ? found
            : Palettes[ArtMaterialKind.GroundGrass];
        var index = variant >= 0 ? variant : StableIndex(color);
        return palette[Mathf.PosMod(index, palette.Length)];
    }

    // Deterministic across runs (string.GetHashCode is randomized), so a given colour always picks the
    // same texture and the world keeps a stable look between launches.
    private static int StableIndex(string value)
    {
        unchecked
        {
            var hash = (int)2166136261;
            foreach (var c in value)
            {
                hash = (hash ^ c) * 16777619;
            }

            return hash & 0x7fffffff;
        }
    }

    private static ArtMaterialKind ResolveCharacter(Color color)
    {
        if (color.A < 0.5f)
        {
            return ArtMaterialKind.Water;
        }

        var max = Mathf.Max(color.R, Mathf.Max(color.G, color.B));
        var min = Mathf.Min(color.R, Mathf.Min(color.G, color.B));
        var saturation = max <= 0.001f ? 0.0f : (max - min) / max;

        if (color.G > color.R * 1.08f && color.G > color.B * 1.08f)
        {
            return color.G > 0.42f ? ArtMaterialKind.GroundGrass : ArtMaterialKind.Leaves;
        }

        if (color.R > color.G * 1.12f && color.G > color.B * 1.08f && color.R > 0.42f)
        {
            return ArtMaterialKind.CharacterSkin;
        }

        if (saturation < 0.18f && max > 0.42f)
        {
            return ArtMaterialKind.Metal;
        }

        if (color.R > color.G && color.G >= color.B && color.R < 0.62f)
        {
            return ArtMaterialKind.Wood;
        }

        if (saturation < 0.16f)
        {
            return ArtMaterialKind.Metal;
        }

        return ArtMaterialKind.CharacterCloth;
    }

    private static ArtMaterialKind ResolveEnvironment(Color color)
    {
        if (color.A < 0.5f)
        {
            return ArtMaterialKind.Water;
        }

        var max = Mathf.Max(color.R, Mathf.Max(color.G, color.B));
        var min = Mathf.Min(color.R, Mathf.Min(color.G, color.B));
        var saturation = max <= 0.001f ? 0.0f : (max - min) / max;

        if (color.G > color.R * 1.08f && color.G > color.B * 1.08f)
        {
            return color.G > 0.42f ? ArtMaterialKind.GroundGrass : ArtMaterialKind.Leaves;
        }

        if (saturation < 0.18f)
        {
            return max > 0.42f ? ArtMaterialKind.Stone : ArtMaterialKind.Metal;
        }

        if (color.R > color.G && color.G >= color.B)
        {
            return color.R > 0.55f && color.G > 0.42f ? ArtMaterialKind.GroundDirt : ArtMaterialKind.Wood;
        }

        return ArtMaterialKind.Props;
    }

    private static float Roughness(ArtMaterialKind kind) =>
        kind switch
        {
            ArtMaterialKind.Water => 0.08f,
            ArtMaterialKind.Metal => 0.42f,
            ArtMaterialKind.CharacterSkin => 0.7f,
            ArtMaterialKind.CharacterHair => 0.74f,
            ArtMaterialKind.Leaves => 0.82f,
            _ => 0.68f,
        };

    private static Vector3 UvScale(ArtMaterialKind kind) =>
        kind switch
        {
            ArtMaterialKind.GroundGrass => new Vector3(14.0f, 14.0f, 1.0f),
            ArtMaterialKind.GroundDirt => new Vector3(10.0f, 10.0f, 1.0f),
            ArtMaterialKind.Water => new Vector3(12.0f, 12.0f, 1.0f),
            ArtMaterialKind.Stone => new Vector3(4.0f, 4.0f, 1.0f),
            ArtMaterialKind.Wood => new Vector3(3.0f, 3.0f, 1.0f),
            ArtMaterialKind.Roof => new Vector3(3.5f, 3.5f, 1.0f),
            ArtMaterialKind.Leaves => new Vector3(2.0f, 2.0f, 1.0f),
            _ => Vector3.One,
        };

    private static Dictionary<ArtMaterialKind, string[]> BuildPalettes()
    {
        var grass = Prefix(Retro, "floor_ground_grass.png", "floor_ground_grass_overlay.png");

        var dirt = Concat(
            Prefix(Retro,
                "floor_ground_dirt.png", "floor_ground_sand.png",
                "floor_tiles_sand_large.png", "floor_tiles_sand_small.png", "floor_tiles_sand_small_damaged.png",
                "floor_tiles_tan_large.png", "floor_tiles_tan_small.png", "floor_tiles_tan_small_damaged.png"),
            Prefix(Kit, "cobblestone.png", "cobblestoneAlternative.png", "cobblestonePainted.png"),
            Prefix(Hex, "hexagons_medieval.png"));

        var stone = Prefix(Retro,
            "wall_brick_stone_both.png", "wall_brick_stone_center.png", "wall_brick_stone_center_banner.png",
            "wall_brick_stone_center_depth.png", "wall_brick_stone_left.png", "wall_brick_stone_right.png",
            "wall_brick_small_stone.png", "wall_brick_small_stone_depth.png",
            "wall_brick_sand_both.png", "wall_brick_sand_center.png", "wall_brick_sand_center_banner.png",
            "wall_brick_sand_center_depth.png", "wall_brick_sand_left.png", "wall_brick_sand_right.png",
            "wall_brick_small_sand.png", "wall_brick_small_sand_depth.png",
            "wall_rock.png", "wall_rock_structure.png", "wall_stone.png", "wall_stone_depth.png",
            "floor_stone.png", "floor_stone_grate.png", "floor_stone_pattern.png", "floor_stone_pattern_depth.png",
            "floor_stone_pattern_small.png", "floor_stone_pattern_small_depth.png", "floor_stone_sand_grate.png",
            "floor_stone_sand_inset.png", "floor_stone_sand_random.png", "floor_stone_sand_random_depth.png",
            "floor_stone_sand_trimsheet.png", "floor_stone_trimsheet.png",
            "floor_tiles_blue_large.png", "floor_tiles_blue_small.png", "floor_tiles_blue_small_damaged.png",
            "window_round_divided.png", "window_round_divided_boarded.png", "window_round_divided_lit.png",
            "window_round_pane.png", "window_round_pane_boarded.png", "window_round_pane_lit.png",
            "window_square_closed.png", "window_square_divided.png", "window_square_divided_boarded.png",
            "window_square_divided_lit.png", "window_square_frame.png", "window_square_horizontal.png",
            "window_square_horizontal_boarded.png", "window_square_horizontal_lit.png", "window_square_pane.png",
            "window_square_pane_boarded.png", "window_square_pane_lit.png", "window_square_vertical.png",
            "window_square_vertical_boarded.png", "window_square_vertical_lit.png");

        var wood = Concat(
            Prefix(Retro,
                "floor_wood_planks.png", "floor_wood_planks_damaged.png", "floor_wood_planks_depth.png",
                "floor_wood_planks_wide.png", "floor_wood_planks_wide_damaged.png", "floor_wood_planks_wide_depth.png",
                "timber_square_clay.png", "timber_square_clay_diagonal.png", "timber_square_frame.png",
                "timber_square_frame_diagonal.png", "timber_square_planks.png", "timber_square_planks_boarded.png",
                "timber_square_planks_cross.png", "timber_square_planks_diagonal.png",
                "wall_timber.png", "wall_timber_structure.png", "wall_timber_structure_cross.png",
                "wall_timber_structure_diagonal.png", "wall_timber_structure_horizontal.png",
                "wall_timber_structure_vertical.png", "wall_wood_trimsheet.png",
                "door_wood.png", "door_wood_frame.png", "door_wood_handle.png", "door_wood_window.png",
                "door_wood_window_lit.png",
                "window_tall_divided.png", "window_tall_divided_damaged.png", "window_tall_divided_lit.png",
                "window_tall_pane.png", "window_tall_pane_lit.png", "window_tall_rounded.png",
                "window_tall_rounded_damaged.png", "window_tall_rounded_lit.png", "window_tall_vertical.png",
                "window_tall_vertical_lit.png"),
            Prefix(Kit, "planks.png"));

        var roof = Concat(
            Prefix(Retro,
                "roof_clay_grey_bottom.png", "roof_clay_grey_center.png", "roof_clay_grey_top.png",
                "roof_clay_red_bottom.png", "roof_clay_red_center.png", "roof_clay_red_top.png",
                "roof_thatch_bottom.png", "roof_thatch_center.png", "roof_thatch_top.png"),
            Prefix(Kit, "roof.png"));

        var water = Concat(
            Prefix(Retro, "floor_ground_water.png", "floor_ground_water_green.png"),
            Prefix(Kit, "water.png"));

        var metal = Concat(
            Prefix(Props, "T_Trim_Metal_BaseColor.png"),
            Prefix(Retro,
                "door_metal_frame.png", "door_metal_gate.png", "door_metal_gate_lock.png",
                "window_square_metal.png", "window_square_metal_fortified.png"));

        var furniture = Concat(
            Prefix(Props, "T_Trim_Furniture_BaseColor.png"),
            Prefix(Kit, "barrel.png", "details.png"));

        var props = Concat(
            Prefix(Props, "T_Trim_Props_BaseColor.png", "T_Trim_Cloth_BaseColor.png", "T_Page_Noise.png"),
            Prefix(Kit, "fence.png", "details.png"));

        var leaves = Concat(
            Prefix(TreePack, "tree04.png", "tree10.png", "tree20.png", "tree30.png"),
            Prefix(Kit, "tree.png"));

        var cloth = Prefix(Outfits,
            "T_Peasant_BaseColor.png", "T_Peasant_2_BaseColor.png", "T_Ranger_BaseColor.png",
            "T_Ranger_3_BaseColor.png", "T_Regular_Female_Dark_BaseColor.png", "T_Regular_Male_Dark_BaseColor.png");

        var skin = Prefix(BaseCharacters,
            "T_Superhero_Male_Dark.png", "T_Superhero_Male_Ligh.png",
            "T_Superhero_Female_Dark_BaseColor.png", "T_Superhero_Female_Light_BaseColor.png");

        var hair = Prefix(BaseCharacters, "T_Hair_1_BaseColor.png", "T_Hair_2_BaseColor.png");

        return new Dictionary<ArtMaterialKind, string[]>
        {
            [ArtMaterialKind.GroundGrass] = grass,
            [ArtMaterialKind.GroundDirt] = dirt,
            [ArtMaterialKind.Stone] = stone,
            [ArtMaterialKind.Wood] = wood,
            [ArtMaterialKind.Roof] = roof,
            [ArtMaterialKind.Water] = water,
            [ArtMaterialKind.Leaves] = leaves,
            [ArtMaterialKind.Bark] = wood,
            [ArtMaterialKind.Metal] = metal,
            [ArtMaterialKind.Furniture] = furniture,
            [ArtMaterialKind.Props] = props,
            [ArtMaterialKind.CharacterCloth] = cloth,
            [ArtMaterialKind.CharacterSkin] = skin,
            [ArtMaterialKind.CharacterHair] = hair,
        };
    }

    private static string[] Prefix(string root, params string[] names)
    {
        var result = new string[names.Length];
        for (var i = 0; i < names.Length; i++)
        {
            result[i] = root + names[i];
        }

        return result;
    }

    private static string[] Concat(params string[][] groups) => groups.SelectMany(group => group).ToArray();

    private static Texture2D? LoadTexture(string path)
    {
        if (TextureCache.TryGetValue(path, out var cached))
        {
            return cached;
        }

        Texture2D? texture = null;
        if (ResourceLoader.Exists(path))
        {
            texture = ResourceLoader.Load<Texture2D>(path);
        }
        else
        {
            var filePath = ProjectSettings.GlobalizePath(path);
            if (System.IO.File.Exists(filePath))
            {
                var image = Image.LoadFromFile(filePath);
                if (image is not null && !image.IsEmpty())
                {
                    texture = ImageTexture.CreateFromImage(image);
                }
            }
        }

        TextureCache[path] = texture;
        return texture;
    }
}
