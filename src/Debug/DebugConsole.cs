using System;
using System.Collections.Generic;

namespace DawnOfBlade.Debug;

public sealed class DebugConsole
{
    private readonly Dictionary<string, Action<IReadOnlyList<string>>> _commands = new();

    public DebugConsole()
    {
        Register("give_item", args => LogCommand("give_item", args));
        Register("set_skill_xp", args => LogCommand("set_skill_xp", args));
        Register("teleport", args => LogCommand("teleport", args));
        Register("unlock_quest", args => LogCommand("unlock_quest", args));
    }

    public void Register(string command, Action<IReadOnlyList<string>> handler)
    {
        _commands[command] = handler;
    }

    public bool TryExecute(string command, IReadOnlyList<string> args)
    {
        if (!_commands.TryGetValue(command, out var handler))
        {
            return false;
        }

        handler(args);
        return true;
    }

    private static void LogCommand(string command, IReadOnlyList<string> args)
    {
        System.Diagnostics.Debug.WriteLine($"Debug command queued: {command} ({string.Join(", ", args)})");
    }
}

