# Just One More Peel — repository instructions

While the owner is away, starting with MAIN-006 (22 November 2026), Codex runs the available checks and decides when plan items are ready to close. A missing owner playtest or sign-off does not block progress. Record the decision as Codex's, identify any checks that could not be performed, and keep actual implementation or verification gaps open. Resume owner review when requested.

When the owner asks to complete a stage in one pass, or two to three at most, carry the implementation and verification through within that limit. Keep working across subtasks without waiting for repeated instructions to continue. Separate owner acceptance from work the agent can finish independently.

Prioritize developing the game. On routine «далее» requests, pick an actionable implementation item that changes the player experience or adds game content. After a change, use the smallest relevant compile check and one focused smoke check when needed. Avoid repeating verification already recorded for unchanged behavior. Run broader sweeps, benchmarks, or builds only when explicitly requested, required for the current deliverable, or warranted by a concrete regression.

## Purpose and authority
3D first-person incremental game about removing layered street notices. One developer; free assets and tools only. Target: a compact playable release for itch.io. The owner likes the current overlapping-paper direction; models and poster variety remain unaccepted.
Use [docs/ROADMAP.md](docs/ROADMAP.md) for the next task. Explicit owner decisions and observed project state govern implementation. Automated checks do not constitute human acceptance.

## Task routes
- Mechanics, tools and economy: [GDD](docs/GDD.md), then PeelGame.cs and PeelStrip.cs.
- Level layouts, working positions and route reachability: [level-design](.agents/skills/level-design/SKILL.md).
- Input, focus, interaction: [ARCHITECTURE](docs/ARCHITECTURE.md), then Starter Assets integration and PeelWorkbench.cs.
- World saves, Continue, migrations and recovery: [save-systems](.agents/skills/save-systems/SKILL.md).
- Models, posters, materials, sound: [ART_DIRECTION](docs/ART_DIRECTION.md) and [ASSET_SOURCES](docs/ASSET_SOURCES.md), then inspect existing assets.
- Verification: [DEVELOPMENT](docs/DEVELOPMENT.md), [PLAYTEST](docs/PLAYTEST.md); performance claims also require [PERFORMANCE](docs/PERFORMANCE.md).
- Windows build size and packed asset analysis: [unity-build-size](.agents/skills/unity-build-size/SKILL.md).
- Windows release handoff and authorized uploads: [itch-publish](.agents/skills/itch-publish/SKILL.md).
- Engineering and structure: [ENGINEERING_STANDARD](docs/ENGINEERING_STANDARD.md).
Load only the relevant route. Project skills are in .agents/skills; their local context and this file override generic examples.

For Codex plugin skill selection and model guidance, see [Development](docs/DEVELOPMENT.md#codex-skills-and-models). Use project Unity skills first and add a plugin skill when its workflow contributes to the task.

## Unity skills

- Use [unity-mcp-skill](.agents/skills/unity-mcp-skill/SKILL.md) for Editor operations and the existing task-specific project skills for implementation.
- Consult [unity-skills](.agents/skills/unity-skills/SKILL.md) for relevant Unity design and package references. [unity-skills-eval](.agents/skills/unity-skills-eval/SKILL.md) documents the upstream benchmark and is relevant only when the owner explicitly asks about that benchmark.
- Both references are sourced from [Unity-Skills at c07ee7f](https://github.com/Besty0728/Unity-Skills/tree/c07ee7f74660def88533278f09ec611bb36e0cbe). Their REST client and benchmark runner require a separate UnitySkills integration. Keep Editor operations on Unity MCP under the current project workflow.

## Plan status maintenance

- Start the user-facing final update with the plan item ID and an explicit status. For a closed item, state the next item and whether it can start. Whenever naming the next actionable item in the final update, include its planned model and explicit reasoning effort from the roadmap. For an open item, state what remains, who needs to act, and which next item can already proceed.
- When a plan item is completed or explicitly closed by the owner, update its status in `docs/MASTER_DEVELOPMENT_PLAN.md` and the current summary in `docs/ROADMAP.md` in the same task. Record owner-closed items as such and do not restart them without a new issue or an explicit request.
- Mark partially completed work as in progress, distinguish implementation, verification and owner acceptance, and preserve relevant failures and remaining checks. Do not mark an entire milestone complete because one subtask is done. Keep the next actionable item current.

## Scope and workflow
- The owner prohibits desktop/computer-use automation. Use Unity MCP for Editor work. A working MCP connection is not permission to control OS windows, mouse or keyboard.
- Inspect relevant source, scene and prefab state before edits. Preserve unrelated changes and Unity GUIDs/.meta. Do not mass-edit scene YAML.
- Keep changes narrow; preserve the imported FirstPerson controller and current URP/Input System packages. No automatic engine/package migrations.
- No paid assets, APIs, subscriptions or generation credits. No new integration or global installation without authorization.
- Do not push, merge, rewrite history or modify remotes without explicit instruction.
- Run focused checks. Do not create ZIPs, release builds or standalone candidates unless directly requested.
- Use the existing Unity MCP setup and task-specific skills. Separate reference research, implementation and verification, with evidence appropriate to each step. For isolated experiments, follow [DEVELOPMENT](docs/DEVELOPMENT.md#isolated-experiments-and-local-history).

## Commits

- Complete each task with one or more local commits after the relevant verification. Split independent logical changes into separate, coherent commits; keep a single commit when the task is one logical change. Do not create empty commits.
- Follow [Conventional Commits 1.0.0](https://www.conventionalcommits.org/en/v1.0.0/): `<type>[optional scope][!]: <description>`. Use `feat` for features, `fix` for bug fixes, and appropriate types such as `docs`, `refactor`, `test`, `perf`, `build`, `ci`, `style`, or `chore` for other changes. Mark breaking changes with `!` or a `BREAKING CHANGE:` footer.
- Write a specific subject describing what the commit changes; add a body after a blank line when needed to explain why, important behavior, or limitations. Avoid vague messages such as `update` or `fix stuff`.
- Inspect the working tree and staged diff. Commit only changes attributable to the task; preserve pre-existing or unrelated work, including changes in shared files, and never stage everything blindly. Report commit IDs and any changes that could not be committed. If verification fails or a commit is blocked, report the failure and leave the affected work intact rather than claiming completion.

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

## Local code graph

- Use Graphify for dependency and impact questions when `graphify-out/graph.json` is available. Start with `graphify query`, `graphify path`, or `graphify explain`, then verify relevant links in source. Check Unity serialization in the actual scenes and prefabs through the existing Unity workflow.
- To build or refresh the local graph, run `$env:GRAPHIFY_BACKEND = 'none'` and `graphify extract Assets --out . --code-only` from the repository root. Refresh when a change makes the graph stale for the task at hand. Keep the existing Unity skills and focused verification workflow.
