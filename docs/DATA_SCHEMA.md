# Data Schema

JSON files are content definitions. IDs should be lowercase snake_case and stable once referenced by saves or other data.

## `data/items/items.example.json`

- `id`: Stable item ID.
- `displayName`: Player-facing item name.
- `description`: Short item description.
- `stackable`: Whether multiple copies occupy one inventory slot.
- `value`: Baseline shop value.

## `data/npcs/npcs.example.json`

- `id`: Stable NPC ID.
- `displayName`: Player-facing NPC name.
- `role`: Short design label.
- `dialogueRootId`: First dialogue node ID.

## `data/skills/skills.example.json`

- `id`: Stable skill ID.
- `displayName`: Player-facing skill name.
- `description`: Skill purpose.
- `maxLevel`: Maximum supported level.

## `data/dialogue/dialogue.example.json`

- `id`: Stable dialogue node ID.
- `speakerId`: NPC or speaker ID.
- `text`: Dialogue text.
- `choices`: List of player choices.
- `choices[].text`: Player-facing choice.
- `choices[].nextNodeId`: Next dialogue node ID or `null` to close.

## `data/quests/quests.example.json`

- `id`: Stable quest ID.
- `title`: Player-facing quest title.
- `description`: Quest summary.
- `objectives`: Ordered objective list.
- `objectives[].id`: Stable objective ID.
- `objectives[].description`: Player-facing objective text.
- `objectives[].requiredCount`: Required progress count.
- `rewards`: Reward tokens such as `xp:skill_id:amount` or `item:item_id:quantity`.

## `data/equipment/equipment.example.json`

Marks an item as wearable and gives it combat bonuses. Keyed by item id.

- `itemId`: Item ID this equipment applies to (must exist in items).
- `slot`: Equipment slot (`Weapon`, `Shield`, `Head`, `Body`, `Legs`, `Hands`, `Feet`, `Cape`, `Amulet`, `Ring`).
- `attackBonus`: Added to melee accuracy.
- `strengthBonus`: Added to max hit.
- `defenseBonus`: Added to evasion.
- `requiredAttack`: Minimum Attack level to wield.
- `requiredDefense`: Minimum Defense level to wear.

## `data/shops/shops.example.json`

- `id`: Stable shop ID.
- `displayName`: Player-facing shop name.
- `stock`: Items the shop trades.
- `stock[].itemId`: Item ID (must exist in items).
- `stock[].quantity`: Starting stock count.
- `stock[].price`: Buy price in coins; sell price is half (floored).

Currency is the item with id `coins`.

## `data/vocabulary/vocabulary.example.json`

- `term`: Source-language term.
- `translation`: Translation in the learner language or fallback target.
- `transliteration`: Optional pronunciation aid.
- `partOfSpeech`: Grammar category.
- `exampleSentence`: Context sentence.
- `difficulty`: Suggested difficulty from 1 upward.

