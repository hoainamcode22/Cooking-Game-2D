# Claude Code Game Studios -- Game Studio Agent Architecture

Indie game development managed through 49 coordinated Claude Code subagents.
Each agent owns a specific domain, enforcing separation of concerns and quality.

## Technology Stack

- **Engine**: Unity 6.3 LTS (6000.3.10f1)
- **Language**: C#
- **Version Control**: Git with trunk-based development
- **Build System**: Unity Build System (IL2CPP for release, Mono for development)
- **Asset Pipeline**: Unity Asset Pipeline v2 + Addressables

> **Note**: Engine-specialist agents exist for Godot, Unity, and Unreal with
> dedicated sub-specialists. Use the Unity set for this project.

## Project Structure

@.claude/docs/directory-structure.md

## Engine Version Reference

@docs/engine-reference/unity/VERSION.md

## Technical Preferences

@.claude/docs/technical-preferences.md

## Coordination Rules

@.claude/docs/coordination-rules.md

## Collaboration Protocol

**User-driven collaboration, not autonomous execution.**
Every task follows: **Question -> Options -> Decision -> Draft -> Approval**

- Agents MUST ask "May I write this to [filepath]?" before using Write/Edit tools
- Agents MUST show drafts or summaries before requesting approval
- Multi-file changes require explicit approval for the full changeset
- No commits without user instruction

See `docs/COLLABORATIVE-DESIGN-PRINCIPLE.md` for full protocol and examples.

> **First session?** If the project has no engine configured and no game concept,
> run `/start` to begin the guided onboarding flow.

## Autopilot & Autonomy Mode

When the user runs **`/autopilot`** (or says **"tiếp tục roadmap"**), the team builds the
game from the master plan with minimal interruptions. In this mode the per-step approval of the
Collaboration Protocol is relaxed into **batch approval with hard safety rails**: additive code/
tool/data/doc work proceeds automatically; anything in the AUTONOMY "STOP LIST" (editing/deleting
scenes, prefabs, key `.asset` data or core logic; commits; spending money/accounts; ambiguous
design calls) still pauses and is collected into a single **"CẦN BẠN"** (needs-you) list.

- Master build plan (A→Z, single source of truth): `production/AUTOPILOT_BACKLOG.md`
- Autonomy rules & safety rails (always obey): @production/AUTONOMY.md
- Complete quest/mission data: `production/MISSIONS_MASTER_LIST.md`

## Coding Standards

@.claude/docs/coding-standards.md

## Context Management

@.claude/docs/context-management.md
