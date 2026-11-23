---
name: itch-publish
description: Prepare or carry out an explicitly requested Just One More Peel Windows release handoff, Butler upload or itch.io publication.
---

# Hand off or upload a Windows release

- Read the [release backlog](../../../docs/DEVELOPMENT_BACKLOG.md) and [RU/EN page draft](../../../docs/ITCH_PAGE_DRAFT.md). Match the verified RL-08 package to its version, Git revision and checksum. Check the actual executable, full Player data, launch instructions, credits and known limitations before handoff.
- Follow [AGENTS.md](../../../AGENTS.md) and the release backlog for build and publication authorization. For an authorized upload, confirm the existing page, its visibility, the intended Windows channel and the version. A push to an existing public channel can make the build downloadable as soon as the upload finishes.
- Reuse the verified payload. If Butler is already available, check its version and supported local validation options. Follow the [official Butler push guide](https://itch.io/docs/butler/pushing.html) for the verified payload and pass the release version via `--userversion`. The owner handles interactive authentication when required.
- After an authorized upload, verify the channel, file version and visibility. Verify the delivered download and launch when accessible under the project's desktop automation rules. Record any verification still pending in the release task.

Adapted for Just One More Peel from the upstream [`itch-publish`](https://github.com/gamedev-skills/awesome-gamedev-agent-skills/blob/c6ca6c41c839b68319ffc08430b99ab768debc32/skills/workflows/itch-publish/SKILL.md) under [Apache 2.0](../_licenses/awesome-gamedev-agent-skills/LICENSE). See the [upstream notice](../_licenses/awesome-gamedev-agent-skills/NOTICE).
