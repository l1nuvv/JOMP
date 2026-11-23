---
name: save-systems
description: Extend or diagnose Just One More Peel world saves, Continue, route state, identifier migrations, or recovery from failed and corrupt writes.
---

# Save and restore a shift

- Read the [world-save architecture](../../../docs/ARCHITECTURE.md) and inspect [PeelWorldSave](../../../Assets/JustOneMorePeel/Scripts/PeelWorldSave.cs), [PeelSaveCoordinator](../../../Assets/JustOneMorePeel/Scripts/PeelSaveCoordinator.cs) and the target route before choosing a format change.
- Persist stable route, stack and gameplay identifiers. Define which values are restored and which can be derived. Route renames and stack splits may have different migration needs. Keep the existing write and recovery policy, including atomic replacement, a readable backup and preservation of unsupported saves.
- Verify affected behavior with an isolated save through Unity MCP. Reuse the relevant cases in [PeelProgressionChecks](../../../Assets/JustOneMorePeel/Editor/PeelProgressionChecks.cs) for ThreeAreaPrototype. Use the actual target scene for a different route. Cover a round trip and, when format or writes change, an older supported save, refusal of a newer version and the affected failure or backup path.
- For the vitrine, restore remaining sheets, revealed fragments and light state around the last-sheet transition. For collections, restore the discovery and sheet progress together. Check that repeated loading or a language change produces a single discovery.

Adapted for Just One More Peel from the upstream [`save-systems`](https://github.com/gamedev-skills/awesome-gamedev-agent-skills/blob/c6ca6c41c839b68319ffc08430b99ab768debc32/skills/disciplines/save-systems/SKILL.md) under [Apache 2.0](../_licenses/awesome-gamedev-agent-skills/LICENSE). See the [upstream notice](../_licenses/awesome-gamedev-agent-skills/NOTICE).
