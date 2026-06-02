# HUD Architecture

Incremental design for the Dawn of Blade client HUD: a Godot 4 `Control` tree bound to the
engine-independent presentation models in `src/UI/Presentation`, fed by authoritative snapshots
that arrive over the communication bus (`src/Communication`) and are paced by the simulation clock
(`src/Simulation`). This is a design document only; no source or scenes are changed by it.

## 1. Principles

1. **Authoritative state, presentational view.** The HUD never computes gameplay truth. It renders
   `src/UI/Presentation` models, which are mutated only by authoritative snapshots. This keeps the
   client safe to desync-correct and lets the same models be unit-tested off-engine (they already are,
   in `tests/HudPresentationTests.cs`).
2. **Bus in, no polling.** Controls subscribe to events on `ICommunicationService`; they do not poll
   `GameManager`. Today the bus is `InProcessCommunicationService`; later a network adapter raises the
   same envelopes, so no HUD code changes when state starts arriving from a server.
3. **Render at framerate, simulate at 600 ms.** Authoritative values change on the simulation tick
   (`SimulationTicked`), but smoothing/trailing animations advance every rendered frame in `_Process`
   using `delta`. The two cadences are kept separate (see §5).
4. **Responsive by anchors, never absolute pixels.** Every panel anchors to a screen edge/corner and
   sizes from theme + content. The current `GameManager` prototype HUD uses absolute `Position`/`Size`
   buttons; this design replaces that with a `Control` tree so nothing spills off-window at any
   resolution. (Migration is incremental — see §7.)

## 2. Control tree

```
HudLayer (CanvasLayer)
└── HudRoot (Control, anchors = Full Rect, mouse_filter = Ignore)
    ├── TopLeftCluster (VBoxContainer, anchor Top-Left, offset 12,12)
    │   ├── VitalsPanel (PanelContainer)
    │   │   ├── HealthGauge (TextureProgressBar ×2: trailing behind, foreground front)
    │   │   ├── PrayerGauge (TextureProgressBar)
    │   │   └── RunEnergyButton (Button + TextureProgressBar fill)
    │   └── CoordinateLabel (Label)            # HudPresentationState.CoordinateText
    ├── MiniChat (PanelContainer, anchor Bottom-Left, offset 12,-12, grow Up/Right)
    │   └── JournalFeed (RichTextLabel, scroll_following = true)
    ├── SidePanel (PanelContainer, anchor Bottom-Right, offset -12,-12, grow Up/Left)
    │   ├── TabBar (HBoxContainer of 4 Buttons)  # HudTab enum
    │   └── TabBody (MarginContainer)
    │       ├── InventoryGrid (GridContainer, 4×7 InventorySlotButton)
    │       ├── EquipmentDoll (GridContainer)
    │       ├── SkillsGrid (GridContainer)       # 99-cap skills
    │       └── QuestJournal (VBoxContainer)
    └── WorldOverlay (Control, anchor Full Rect, mouse_filter = Ignore)
        └── HitMarkerPool (N pooled Label nodes, positioned per frame)
```

Notes:
- `HudRoot` and `WorldOverlay` use `mouse_filter = Ignore` so only interactive widgets (buttons,
  slots) consume clicks; world click-to-move still reaches the viewport everywhere else.
- `SidePanel` and `MiniChat` grow *inward* from their anchored corner, so they never clip when the
  window shrinks; `TabBody` has a `CustomMinimumSize` and the panel sizes to content.
- The HUD lives on its own `CanvasLayer` so it is unaffected by the 3D camera and the
  `canvas_items` stretch mode keeps it crisp across resolutions.

## 3. Model ↔ control bindings

| Presentation model (`src/UI/Presentation`) | Control | Binding |
| --- | --- | --- |
| `HudPresentationState.CoordinateText` | `CoordinateLabel.Text` | set on each `VerifiedTile` apply |
| `HudPresentationState.ActiveTab` (`HudTab`) | `TabBar` toggle + `TabBody` page | `SelectTab` on button press; show matching page |
| `VitalGaugeState` (health/prayer) | `HealthGauge` (two bars) | foreground = `Value`, trailing = `TrailingValue`, both /`Maximum` |
| `RunEnergyState` | `RunEnergyButton` | fill = `Energy`/100; `Disabled = !CanToggleRun`; pressed look = `IsRunning` |
| `HitMarkerPresentation` | pooled `Label` in `HitMarkerPool` | text = `Damage` (or "miss"), color by `Type`, x += `HorizontalOffset`, freed after `LifetimeSeconds` |

The models already encode the tricky behavior, so the Controls stay dumb:
- **Delayed trailing health.** `VitalGaugeState.Apply` arms a 0.3 s delay then `Advance(delta)` drains
  `TrailingValue` toward `Value` over ~0.5 s. The red "lag bar" is just the trailing bar rendered
  behind the foreground bar; the HUD calls `Advance(delta)` in `_Process`.
- **Run-energy exhaustion/re-enable.** `RunEnergyState.ApplyAuthoritative` forces walk at 0 energy and
  only re-permits running at ≥ `ReenableThreshold` (15). The button binds `CanToggleRun` to `Disabled`;
  it never decides this locally.
- **Hit markers.** `HitMarkerPresentation.Create` clamps offset to ±20 and sets lifetime by type
  (crit = 1.0 s, else 0.8 s). The pool spawns a label, lerps it upward over its lifetime, recycles it.

## 4. Event binding (client side)

The HUD subscribes once on ready and disposes on tree exit. Suggested authoritative events
(published by the simulation/netcode layer; names illustrative, all `: IEvent`):

```
ICommunicationService bus;   // injected; today InProcessCommunicationService

_subs.Add(bus.Subscribe<VitalsChanged>((e,_) => { _health.Apply(e.Message.Hp, e.Message.MaxHp); return default; }));
_subs.Add(bus.Subscribe<RunEnergyChanged>((e,_) => { _run.ApplyAuthoritative(e.Message.Energy, e.Message.Running); return default; }));
_subs.Add(bus.Subscribe<TileVerified>((e,_) => { _state.ApplyVerifiedTile(e.Message.Tile); return default; }));
_subs.Add(bus.Subscribe<DamageDealt>((e,_) => { SpawnHitMarker(e.Message); return default; }));
_subs.Add(bus.Subscribe<JournalAppended>((e,_) => { _journal.AppendText(e.Message.Line + "\n"); return default; }));
_subs.Add(bus.Subscribe<SimulationTicked>((e,_) => { /* optional: tick-aligned refresh */ return default; }));
```

Input flows the other way as commands, never as direct state mutations:
- Tab button → `_state.SelectTab(tab)` (pure local view state; safe to apply immediately).
- Run toggle → publish a `ToggleRunRequested` command onto the bus / simulation buffer; the button's
  enabled state still comes only from the next authoritative `RunEnergyChanged`.
- Inventory slot drag → publish `InventorySwapRequest(from, to)`; the grid re-renders only when the
  authoritative inventory snapshot returns (matches the server's atomic-transaction rule).

This preserves the blueprint's server-authoritative contract: the HUD shows verified state and sends
intents; it never grants itself items, energy, or hits.

## 5. Two-clock update model

```
_Process(delta):                      # every rendered frame (60+ fps)
    _health.Advance(delta)            # trailing-bar catch-up
    _prayer.Advance(delta)
    AdvanceHitMarkers(delta)          # rise + fade + recycle
    InterpolateNothingGameplay()      # HUD has no gameplay sim

on SimulationTicked / authoritative event:   # ~every 600 ms or on change
    apply snapshot to models (Apply / ApplyAuthoritative / ApplyVerifiedTile)
```

Animations are frame-paced; truth is tick-paced. A dropped or late snapshot never corrupts the view —
the next authoritative apply snaps the models back, exactly like the world-space rubber-banding.

## 6. Zero hand-holding constraints (from the blueprint)

The HUD deliberately omits a minimap, floating waypoints, glowing target markers, and auto quest
tracking. `JournalFeed` is the only navigation aid: it renders raw authoritative hint strings
(`JournalAppended`). The coordinate readout (`HudPresentationState.CoordinateText`) is the player's
only positioning instrument, satisfying Part 6.2 while remaining honest about location.

## 7. Incremental adoption plan

1. **Extract the HUD from `GameManager`.** Add a `HudController : CanvasLayer` scene that owns the
   `Control` tree above and the presentation models; have `GameManager` instantiate it and forward the
   bus. (No gameplay logic moves; only presentation.)
2. **Bind read-only panels first** — coordinates, vitals, run energy — driven by existing/temporary
   events. These are pure projections of tested models, so they cannot regress gameplay.
3. **Add the tabbed `SidePanel`** (inventory/equipment/skills/journal) rendering current
   `GameManager` state through adapter events; keep the old prototype buttons until parity is reached.
4. **Move input to commands** — once `src/Simulation` is consumed by gameplay, route HUD actions
   (run toggle, inventory swap) as `ISimulationCommand`s scheduled for the next tick.
5. **Delete the absolute-positioned prototype HUD** in `GameManager.BuildPrototypeHud` only after the
   responsive tree is at parity.

## 8. Files (when implemented — not part of this doc)

- `scenes/UI/Hud.tscn` — the `Control` tree.
- `src/UI/HudController.cs` — subscribes to the bus, owns the presentation models, drives `_Process`.
- `src/UI/Widgets/*` — thin per-widget scripts (vital gauge, run button, slot grid, hit-marker pool).
- Reuses, unchanged: `src/UI/Presentation/*`, `src/Communication/*`, `src/Simulation/*`,
  `src/World/Grid/GridCoordinate.cs`.
