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
- Combat resolution — accuracy/damage, attack styles, the combat triangle (`CombatTests`).
- Equipment bonuses, slots, and level requirements (`EquipmentTests`).
- Shop buy/sell pricing and stock (`ShopServiceTests`).
- Quest progress, completion, reward-token parsing, and quest log (`QuestTests`).
- Save model JSON serialization round-trip (`SaveSerializerTests`).
- Definition parsing plus schema validation of the real `data/*.json` files
  (`DefinitionParseTests`, `ContentIntegrityTests`).
- Character appearance and NPC/world generation (`CharacterWorldTests`).
- Grid world — tiles, 32×32 chunks, interest filtering, A* pathing, line-of-sight, and zone rules
  (`GridWorldTests`, `AdvancedWorldRulesTests`).
- HUD presentation state — gauges, run energy, tabs, hit markers (`HudPresentationTests`).
- Communication bus dispatch and gameplay events (`CommunicationServiceTests`, `GameplayEventsTests`).
- Deterministic simulation tick loop — buffering, late-command deferral, ordering, clock
  (`SimulationTests`).

> Note: the test project is intentionally kept out of `DawnOfBlade.sln` so the Godot
> editor's build stays focused on the game assembly. Run it with the command above.
