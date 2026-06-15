# Shared Prompt - Level-Up Lana VFX Integration

## Project

- Root: `E:\Game2\Cooking-Game-2D`
- Engine: Unity `6000.3.10f1`
- Target scene: `Assets/_Game/Scenes/SCN_Farm.unity`
- Source scenes:
  - `Assets/Lana Studio/Hyper Casual FX/Demo/Scenes/LanaDemo02.unity`
  - `Assets/Lana Studio/Hyper Casual FX/Demo/Scenes/LanaDemo03.unity`
- Existing level-up implementation:
  - `Assets/_Game/Farm/Scripts/UI/LevelUpPopupUI.cs`
  - `Assets/_Game/Farm/Editor/LevelUpPopupSetupTool.cs`

## Player And Product Goal

The farming/cooking game primarily targets women and children. The presentation
must feel cozy, cheerful, easy to understand, and continuously rewarding. Level
up moments should use richer animation and VFX without making the popup hard to
read or causing discomfort.

## Requested Result

1. Reuse the celebration hierarchy represented in `LanaDemo02` and
   `LanaDemo03`.
2. Use the `LanaDemo02` multicolor confetti/firework effect above the level-up
   popup.
3. Use the `LanaDemo03` blue/pink magic flash effect on the left and right
   sides of the popup.
4. Keep all three effects outside the popup content panel so they decorate the
   background without covering reward text, icons, or the claim button.
5. Copy the selected Lana prefabs into a game-owned VFX folder so the level-up
   feature does not directly depend on demo-scene hierarchy.
6. Add a clear hierarchy under `LevelUpPopup` in `SCN_Farm`.
7. Make the setup idempotent and safe to rerun through a Unity Editor tool.

## Required Hierarchy

```text
LevelUpPopup
|-- VFX_Background
|   |-- VFX_Top_Lana02
|   |   `-- LevelUp_Confetti_Lana02
|   |-- VFX_Left_Lana03
|   |   `-- LevelUp_Flash_Lana03_Left
|   `-- VFX_Right_Lana03
|       `-- LevelUp_Flash_Lana03_Right
`-- ContentPanel
```

Hierarchy sibling order must keep `VFX_Background` behind `ContentPanel`.

## Technical Constraints

- Preserve all existing user changes in `SCN_Farm.unity`.
- Do not replace or recreate the current `LevelUpPopup`.
- Do not change level rewards, progression, economy, tutorial, or claim logic.
- Keep the feature compatible with the existing `LevelUpPopupUI` event flow.
- Avoid duplicate VFX children or duplicate prefab copies when setup is rerun.
- VFX must play when the popup opens and stop when it closes.
- Use unscaled popup animation timing as already implemented.
- Keep particle count and overdraw reasonable for mobile.
- Preserve Lana source assets; do not delete or modify the originals.

## Source Effects

- LanaDemo02 selected source:
  `Assets/Lana Studio/Hyper Casual FX/Prefabs/Confetti/Confetti_blast_multicolor.prefab`
- LanaDemo03 selected source:
  `Assets/Lana Studio/Hyper Casual FX/Prefabs/Flash/Flash_magic_blue_pink.prefab`

## Game-Owned Destination

```text
Assets/_Game/Farm/Prefabs/VFX/LevelUp/
|-- LevelUp_Confetti_Lana02.prefab
`-- LevelUp_Flash_Lana03.prefab
```

## Agent Workstreams

| Role | Responsibility | Expected output |
|---|---|---|
| Producer | Scope, task order, acceptance criteria | Short integration checklist |
| Creative/Game/Art/UX | Audience fit and celebration composition | Placement/readability recommendations |
| Technical Director/Lead Programmer | Integration architecture and risks | Technical decision notes |
| Unity Specialist | Scene, prefab, particle lifecycle | Unity implementation review |
| Unity UI Specialist/UI Programmer | Canvas, anchors, sibling order | UI hierarchy review |
| Technical Artist | Particle scale, color, sorting, overdraw | VFX tuning notes |
| Tools Programmer | Idempotent Editor setup | Tool behavior review |
| QA Lead/QA Tester | Regression and play-mode checks | Test checklist |
| Performance Analyst | Mobile cost and cleanup | Performance checks |
| Accessibility Specialist | Flash/readability comfort | Accessibility checks |

## Acceptance Criteria

- `SCN_Farm` contains one `LevelUpPopup`.
- `LevelUpPopup` contains one `VFX_Background` with top, left, and right anchors.
- The top effect uses the game-owned LanaDemo02 confetti copy.
- The side effects use the game-owned LanaDemo03 flash copy.
- Effects are visually outside and behind `ContentPanel`.
- Existing popup references, reward configs, and claim button remain intact.
- Opening the popup replays all effects.
- Closing the popup clears/stops active effects.
- Re-running the setup produces no duplicates.
- Unity scripts compile with no errors.

