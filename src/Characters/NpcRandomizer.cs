using System;

namespace DawnOfBlade.Characters;

/// <summary>A generated NPC: a name, a role, a dialogue entry point, and an appearance.</summary>
public sealed record NpcInstance(string Name, string Role, string DialogueRootId, Appearance Appearance);

/// <summary>
/// Deterministically generates varied NPCs from a seed, drawing appearance from
/// <see cref="AppearanceOptions"/>. Same seed always yields the same NPC, which keeps world
/// population stable across sessions.
/// </summary>
public sealed class NpcRandomizer
{
    private static readonly string[] FirstNames =
    {
        "Bram", "Elspeth", "Doran", "Mira", "Kell", "Suri",
        "Garrin", "Othro", "Lena", "Tibbs", "Wynn", "Hask",
    };

    private static readonly string[] Roles =
    {
        "Villager", "Guard", "Merchant", "Farmer", "Wanderer",
    };

    public NpcInstance Generate(int seed, string dialogueRootId = "mira_intro")
    {
        var random = new Random(seed);

        var appearance = new Appearance
        {
            Presentation = Pick(random, AppearanceOptions.Presentations),
            BodyType = Pick(random, AppearanceOptions.BodyTypes),
            HeadStyle = random.Next(AppearanceOptions.ShapeStyleCount),
            JawStyle = random.Next(AppearanceOptions.ShapeStyleCount),
            TorsoStyle = random.Next(AppearanceOptions.ShapeStyleCount),
            ArmStyle = random.Next(AppearanceOptions.ShapeStyleCount),
            HandStyle = random.Next(AppearanceOptions.ShapeStyleCount),
            LegStyle = random.Next(AppearanceOptions.ShapeStyleCount),
            FootStyle = random.Next(AppearanceOptions.ShapeStyleCount),
            HairStyle = random.Next(AppearanceOptions.HairStyleCount),
            SkinTone = Pick(random, AppearanceOptions.SkinTones),
            HairColor = Pick(random, AppearanceOptions.HairColors),
            ShirtColor = Pick(random, AppearanceOptions.ShirtColors),
            LegColor = Pick(random, AppearanceOptions.LegColors),
            FootColor = Pick(random, AppearanceOptions.FootColors),
        };

        var name = FirstNames[random.Next(FirstNames.Length)];
        var role = Roles[random.Next(Roles.Length)];
        return new NpcInstance(name, role, dialogueRootId, appearance);
    }

    private static string Pick(Random random, string[] choices) => choices[random.Next(choices.Length)];
}
