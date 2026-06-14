# Shared Prompt - Rebuild Tutorial Hands L1-L2

## Goal

Rebuild the deleted tutorial hand hierarchy in `SCN_Farm` and make the Level 1
flow playable without dialog panels blocking plot, diamond, seed drag, or sickle
input.

## Required Flow

1. Guide board starts hidden. Confirm button says `Da hieu`.
2. Click hand points to one of the six tutorial rice plots.
3. Opening the seed panel advances to the drag hand.
4. Drag hand loops from the rice seed to the plot until a real plant event.
5. After all six rice plots are planted, one dialog click hides the dialog.
6. Action hand points to the growing plot, then follows the diamond button.
7. A real speed-up click advances the tutorial.
8. Harvest dialog closes with one click. Action hand points to the ready plot,
   then follows the visible sickle tray. The dialog/dim cannot cover the sickle.
9. Repeat the same interaction for all six flower pots.

## Hierarchy Contract

`Tutorial_Canvas/Tutorial_Hands` contains:

- `Hand_Click_Plot`
- `Hand_Drag_Seed`
- `Hand_Action_Plot_Diamond_Sickle`

All hand graphics have `raycastTarget = false`. The setup tool imports and
assigns `Assets/_Game/Farm/Art/UI/tutorial_hand.png`, wires references, assigns
exactly six normal plots plus six flower plots, and saves `SCN_Farm`.

## Team Review

- State-machine audit: Descartes
- UI/hierarchy audit: Cicero
- World/plot audit: Sagan
- Integration and verification: Codex
