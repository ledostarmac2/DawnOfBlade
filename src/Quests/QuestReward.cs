namespace DawnOfBlade.Quests;

/// <summary>
/// A parsed quest reward token. Tokens look like <c>xp:foraging:25</c> or
/// <c>item:practice_chisel:1</c> — see <c>docs/DATA_SCHEMA.md</c>.
/// </summary>
public readonly record struct QuestReward(string Kind, string Target, int Amount)
{
    public const string KindXp = "xp";
    public const string KindItem = "item";

    public static bool TryParse(string? token, out QuestReward reward)
    {
        reward = default;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var parts = token.Split(':');
        if (parts.Length != 3 || !int.TryParse(parts[2], out var amount))
        {
            return false;
        }

        reward = new QuestReward(parts[0].Trim(), parts[1].Trim(), amount);
        return true;
    }
}
