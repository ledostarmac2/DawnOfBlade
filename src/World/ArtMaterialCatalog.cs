using System.Collections.Generic;
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

/// <summary>Central map from procedural material intent to the texture packs under res://textures.</summary>
public static class ArtMaterialCatalog
{
    private const string Retro = "res://assets/runtime_textures/kenney_retro_textures_fantasy/";
    private const string Props = "res://assets/runtime_textures/quaternius_props/";
    private const string Outfits = "res://assets/runtime_textures/quaternius_outfits/";
    private const string BaseCharacters = "res://assets/runtime_textures/quaternius_base_characters/";
    private const string TreePack = "res://assets/runtime_textures/tree_pack/";

    private static readonly Dictionary<string, Texture2D?> TextureCache = new();

    public static StandardMaterial3D Create(string color, ArtMaterialKind kind = ArtMaterialKind.Auto)
    {
        var resolved = kind == ArtMaterialKind.Auto ? ResolveCharacter(new Color(color)) : kind;
        return CreateResolved(color, resolved);
    }

    public static StandardMaterial3D Environment(string color, ArtMaterialKind kind = ArtMaterialKind.Auto)
    {
        var resolved = kind == ArtMaterialKind.Auto ? ResolveEnvironment(new Color(color)) : kind;
        return CreateResolved(color, resolved);
    }

    private static StandardMaterial3D CreateResolved(string color, ArtMaterialKind resolved, string? texturePath = null)
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

        var texture = LoadTexture(texturePath ?? AlbedoPath(resolved));
        if (texture is not null)
        {
            material.AlbedoTexture = texture;
        }

        return material;
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
        return CreateResolved(color, ArtMaterialKind.Leaves, TreePack + $"tree{clamped:00}.png");
    }

    public static StandardMaterial3D BushLeaves(string color, int variant)
    {
        var clamped = Mathf.Clamp(variant, 1, 8);
        return CreateResolved(color, ArtMaterialKind.Leaves, TreePack + $"bush{clamped:00}.png");
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

    private static string AlbedoPath(ArtMaterialKind kind) =>
        kind switch
        {
            ArtMaterialKind.CharacterCloth => Outfits + "T_Peasant_BaseColor.png",
            ArtMaterialKind.CharacterSkin => BaseCharacters + "T_Superhero_Male_Dark.png",
            ArtMaterialKind.CharacterHair => BaseCharacters + "T_Hair_1_BaseColor.png",
            ArtMaterialKind.GroundGrass => Retro + "floor_ground_grass.png",
            ArtMaterialKind.GroundDirt => Retro + "floor_ground_dirt.png",
            ArtMaterialKind.Stone => Retro + "wall_brick_stone_center.png",
            ArtMaterialKind.Wood => Retro + "floor_wood_planks.png",
            ArtMaterialKind.Roof => Retro + "roof_thatch_center.png",
            ArtMaterialKind.Water => Retro + "floor_ground_water.png",
            ArtMaterialKind.Leaves => TreePack + "tree04.png",
            ArtMaterialKind.Bark => Props + "T_Trim_Furniture_BaseColor.png",
            ArtMaterialKind.Metal => Props + "T_Trim_Metal_BaseColor.png",
            ArtMaterialKind.Furniture => Props + "T_Trim_Furniture_BaseColor.png",
            ArtMaterialKind.Props => Props + "T_Trim_Props_BaseColor.png",
            _ => Retro + "floor_ground_grass.png",
        };

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
