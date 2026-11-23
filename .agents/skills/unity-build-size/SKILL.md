---
name: unity-build-size
description: Investigate Just One More Peel Windows x64 Player or download size using an existing build, packed asset list, or Unity Build Report.
---

# Measure Windows build size

- Start with the [Windows build profile and evidence workflow](../../../docs/DEVELOPMENT.md#окружение-сборки-windows-x64-te-09) and [release backlog](../../../docs/DEVELOPMENT_BACKLOG.md). Identify the artifact revision and whether the question concerns the installed folder, the download or a section of the Unity Build Report. Keep each metric in its own column.
- Reuse existing evidence. Use the packed asset list or Build Report to rank the biggest asset groups and paths. When the report is missing, measure the existing folder's largest files and state which asset-level attribution is missing.
- Trace the largest suspect paths back to scenes, prefabs and runtime loads, including `Resources` and `StreamingAssets`. Check the actual Standalone import overrides and preserve assets used at runtime and required credits and notices. Use Unity MCP for Editor operations.
- Compare like-for-like revisions, profile, scene list and size metric. For asset import changes, verify gameplay and visual quality from the normal entry point. Follow the build authorization in [AGENTS.md](../../../AGENTS.md) when a fresh Player or package is needed.

Adapted for Just One More Peel from the upstream [`unity-build-size`](https://github.com/gamedev-skills/awesome-gamedev-agent-skills/blob/c6ca6c41c839b68319ffc08430b99ab768debc32/skills/unity/unity-build-size/SKILL.md) under [Apache 2.0](../_licenses/awesome-gamedev-agent-skills/LICENSE). See the [upstream notice](../_licenses/awesome-gamedev-agent-skills/NOTICE).
