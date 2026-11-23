---
name: domain-modeling
description: Clarify Just One More Peel gameplay rules shared by peeling, progression, economy, collections or saves. Use when a change introduces or changes a gameplay state, identifier or completion rule across systems.
---

# Gameplay domain modeling

Read the relevant parts of the [GDD](../../../docs/GDD.md), [architecture](../../../docs/ARCHITECTURE.md) and [current task](../../../docs/ROADMAP.md). Check the terms and rules against the relevant C# and scene wiring. Use [Graphify](../graphify/SKILL.md) when tracing dependencies between systems.

- Describe the rule through player actions and observable states. Distinguish a strip, a completed sheet and a stack. If a change affects persistence, identify what is saved, what is derived and which identifier stays stable across tools, reloads and RU/EN.
- Use concrete cases to test the rule before implementation. For MAIN-011, leave one sheet, remove the last sheet, use the light and reload before and after activation. For HUB-102, check partial peel, full peel by different tools, repeated discovery and reload in another language.
- Record the agreed rules and acceptance cases in the relevant existing task, GDD or architecture section. If scope is limited to clarifying a rule, update only the relevant text.

Adapted from [Matt Pocock's domain-modeling](https://github.com/mattpocock/skills/blob/abeeaf95660ba9e8b7c753eb38f441ca707a8823/skills/engineering/domain-modeling/SKILL.md) under the [MIT license](LICENSE).
