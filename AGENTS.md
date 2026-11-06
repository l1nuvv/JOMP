# Just One More Peel — repository instructions

When the owner asks to complete a stage in one pass, or two to three at most, carry the implementation and verification through within that limit. Keep working across subtasks without waiting for repeated instructions to continue. Separate owner acceptance from work the agent can finish independently.

## Purpose and authority
3D first-person incremental game about removing layered street notices. One developer; free assets and tools only. Target: a compact playable release for itch.io. The owner likes the current overlapping-paper direction; models and poster variety remain unaccepted.
Use [docs/ROADMAP.md](docs/ROADMAP.md) for the next task. Explicit owner decisions and observed project state govern implementation. Automated checks do not constitute human acceptance.

## Task routes
- Mechanics, tools and economy: [GDD](docs/GDD.md), then PeelGame.cs and PeelStrip.cs.
- Input, focus, interaction: [ARCHITECTURE](docs/ARCHITECTURE.md), then Starter Assets integration and PeelWorkbench.cs.
- Models, posters, materials, sound: [ART_DIRECTION](docs/ART_DIRECTION.md) and [ASSET_SOURCES](docs/ASSET_SOURCES.md), then inspect existing assets.
- Verification: [DEVELOPMENT](docs/DEVELOPMENT.md), [PLAYTEST](docs/PLAYTEST.md); performance claims also require [PERFORMANCE](docs/PERFORMANCE.md).
- Engineering and structure: [ENGINEERING_STANDARD](docs/ENGINEERING_STANDARD.md).
Load only the relevant route. Project skills are in .agents/skills; their local context and this file override generic examples.

## Plan status maintenance
- When a plan item is completed or explicitly closed by the owner, update its status in `docs/MASTER_DEVELOPMENT_PLAN.md` and the current summary in `docs/ROADMAP.md` in the same task. Record owner-closed items as such and do not restart them without a new issue or an explicit request.
- Mark partially completed work as in progress, distinguish implementation, verification and owner acceptance, and preserve relevant failures and remaining checks. Do not mark an entire milestone complete because one subtask is done. Keep the next actionable item current.

## Scope and workflow
- The owner prohibits desktop/computer-use automation. Use Unity MCP for Editor work. A working MCP connection is not permission to control OS windows, mouse or keyboard.
- Inspect relevant source, scene and prefab state before edits. Preserve unrelated changes and Unity GUIDs/.meta. Do not mass-edit scene YAML.
- Keep changes narrow; preserve the imported FirstPerson controller and current URP/Input System packages. No automatic engine/package migrations.
- No paid assets, APIs, subscriptions or generation credits. No new integration or global installation without authorization.
- Do not push, merge, rewrite history or modify remotes without explicit instruction. Do not force a commit just to complete a task.
- Run focused checks. Do not create ZIPs, release builds or standalone candidates unless directly requested.
- Use the existing Unity MCP setup and task-specific skills. Separate reference research, implementation and verification, with evidence appropriate to each step. For isolated experiments, follow [DEVELOPMENT](docs/DEVELOPMENT.md#isolated-experiments-and-local-history).

## Asset quality and sourcing
- Inspect suitable assets already in this project and authorized source projects before making assets.
- Search relevant free sources next: Poly Haven / Kenney / Quaternius for models; Poly Haven / ambientCG for materials; Kenney / Freesound for audio; Mixamo / Quaternius for animation.
- Verify actual downloads, commercial-use/modification rights, attribution, formats, scale, pivots, separate parts and URP compatibility. Record adopted assets in ASSET_SOURCES; inaccessible sources are not checked sources.
- If no suitable asset exists, record the concrete mismatch and use justified adaptation or authored work. Do not prolong searching needlessly.
- For authored tools and posters, establish a concrete visual reference and an in-game framing before detailed production. For animation, inspect existing compatible rigs and poses before authoring or importing new ones. Follow [ART_DIRECTION](docs/ART_DIRECTION.md#постановка-инструментов-и-плакатов).
- The owner rejected box-based scenery/tools as final artwork. Do not substitute a pile of primitives or procedural pixel shapes for requested models or finished posters. Existing greybox may remain for logic tests while explicitly marked as temporary.
- Preserve approved 3D volume, matte materials, restrained colours and readable first-person scale. Paper should look pasted onto a specific lived-in wall, with intentional overlaps, different formats and remnants.
- Human-looking craft requires coherent composition and concrete details; random placement or generated imagery alone does not establish quality.
- Integrate and inspect actual chosen assets from the playable camera. A downloaded file alone is not delivery.

## Verification and reporting
- Separate implementation, compilation, automated scene checks and owner playtest acceptance.
- Check Console after script compilation; inspect meaningful behavior, not just existence of components.
- For cursor work test startup, clicking Game view, focus return, pause/shop open-close and restart.
- Test continuous held input across layer transitions and workbench range, facing and occlusion.
- Preserve evidence of failures and state what remains unverified. Do not promise optimization from draw-call counts or Editor screenshots alone.
- Report the result, checks and limits concisely. Source-project gate labels, city economy, release history and approvals do not apply here.
