---
name: tutorial-l1-l2-phase
description: Tutorial L1→L2 implementation status — steps, EXP analysis, tools created
metadata:
  type: project
---

Tutorial L1→L2 phase implemented 2026-06-12.

**EXP Analysis:**
- Level 1→2 needs: 40 EXP (formula: 40 + (n-1)*10 + (n-1)^2, n=1)
- 1 rice harvest: 5 EXP; 6 plots: 30 EXP
- **SHORTFALL: 10 EXP** — 6 rice plots NOT enough for Level 2
- Need 8 plots OR 1 train delivery (10 EXP/slot)

**Why:** User explicitly requested L1→L2 tutorial, EXP calc was a required deliverable before implementing.

**How to apply:** When designing future tutorial extensions, note that 6 rice plots alone won't trigger Level 2. User must approve adding flower plot step or delivery step.

**Files modified (additive only):**
- TutorialStepData.cs — added 5 new WaitAction values + showGuideBoard field
- TutorialManager.cs — added _guideBoardUI, guide board logic in PlayStep, 5 new Notify methods
- FarmManager.cs — added OnPlotHarvestedEvent static event + invocation

**Files created:**
- TutorialGuideBoardUI.cs — 4-step guide popup with image slots + confirm button
- TutorialStepTriggerBridge.cs — bridges FarmManager events to TutorialManager
- SetupTutorialL1L2Tool.cs — Editor: Tools/Farm Game/Setup Tutorial L1-L2
- CheckTutorialL1L2SetupTool.cs — Editor: Tools/Farm Game/Test/Check Tutorial L1-L2 Setup

**Step assets location:** Assets/Resources/TutorialSteps/L1_L2/ (11 steps: L1L2_01 → L1L2_11)

**Manual work remaining:**
1. Run Tools/Farm Game/Setup Tutorial L1-L2 in Unity
2. Drag 11 step assets into TutorialManager._steps in order
3. Assign NPC portrait sprite to TutorialManager._npcPortrait
4. Assign 4 illustration images to Tutorial_GuideBoard/ContentPanel/StepCards/StepCard_N/IllustrationImage
5. Verify LevelReward_L2.asset is in LevelUpPopupUI.levelRewardConfigs list
