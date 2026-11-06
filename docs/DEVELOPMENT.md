# Development workflow
Use Unity MCP for Editor tasks; do not drive the desktop. Read AGENTS.md and the relevant documentation route.
Preserve the current engine, pipeline and package baseline. Inspect actual APIs with available reflection/docs tools; generic skill snippets are examples, not guaranteed signatures.
Read relevant files and scene links before changing them. Protect GUIDs. Inspect Git state and focused diffs where a Git workflow is available; never claim those checks if not run.
## Verification levels
1. Written: source/assets changed.
2. Compiled: Unity completes compilation; Console checked.
3. Automated scene check: invariants exercised through Unity MCP in Play Mode.
4. Human playtest: owner operates the actual game.
5. Player build/release: separately requested build and platform checks.
Each level has distinct evidence. A simulated method call is not a physical mouse test.
Do not widen testing after the relevant checks pass unless a new failure/change justifies it. No routine release packaging.
## Isolated experiments and local history

- Для обычной очереди достаточно текущего рабочего дерева. Для независимой пробы с отдельными изменениями используй отдельный `git worktree`, если затраты на отдельный импорт Unity оправданы. Перед запуском проверки сверяй путь проекта и экземпляр Unity MCP. Для параллельных проб заранее разделяй файлы и сцены.
- Перед изменениями проверь локальный статус. Если задача включает коммит, фиксируй проверенный результат отдельным коммитом по задаче. Чужие изменения и незавершённые пробы сохраняй отдельно.
- Для разбора чужой игры сформулируй один вопрос и запиши наблюдение по доступному игровому процессу, затем проверь свою реализацию. Если нужен пример кода, используй открытый исходник с проверенной совместимой лицензией. Для двери сравни обозначение обязательной зоны, для отрыва — момент визуального и звукового отклика.

## Imported skills
12 reusable skill folders were adapted from Hustle & Scale. Their project context takes precedence over generic examples. Load only the applicable skill and its needed references. Uninstalled related skills are optional suggestions, not dependencies to install automatically.
