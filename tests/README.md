# Tests

Unit tests live in `DawnOfBlade.Tests.csproj` (xUnit, net8.0). They cover the
engine-independent gameplay logic so they run without Godot.

```powershell
dotnet test tests/DawnOfBlade.Tests.csproj
```

The project references the game project for the types under test but only exercises
pure C# (no engine calls), so the Godot editor is not required.

## Coverage

- Inventory add/remove behavior (`InventoryTests`).
- Skill XP curve and level calculation (`SkillProgressTests`).
- Quest progress, completion, reward-token parsing, and quest log (`QuestTests`).
- Save model JSON serialization round-trip (`SaveSerializerTests`).
- Definition parsing plus schema validation of the real `data/*.json` files
  (`DefinitionParseTests`).

> Note: the test project is intentionally kept out of `DawnOfBlade.sln` so the Godot
> editor's build stays focused on the game assembly. Run it with the command above.
