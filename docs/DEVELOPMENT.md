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

## Окружение сборки Windows x64 (TE-09)

| Параметр | Значение |
|---|---|
| Unity Editor | `6000.6.0f1` (`f7f8ed4d1e24`), из `ProjectSettings/ProjectVersion.txt` |
| Профиль | `Assets/Settings/Build Profiles/Windows Standard.asset`, Windows x64 Player, Mono. Development Build, Script Debugging и Autoconnect Profiler выключены. |
| Сцена | `Assets/JustOneMorePeel/Scenes/ThreeAreaPrototype.unity` — единственная включённая сцена профиля и глобального списка, индекс 0. |
| Графика | Уровень качества `PC`, `PC_RPAsset`, Direct3D 11 первым, Direct3D 12 вторым, автоматический список API выключен. |
| Пакеты | Версии определяют `Packages/manifest.json` и `Packages/packages-lock.json`. URP и SRP Core `17.6.0`, Input System `1.20.1`, Cinemachine `6.6.0`, MCP закреплён на `d49ae2953580f3481beb1e084a1da2682f0b5610`. |

1. Открыть чистую копию выбранной ревизии на согласованной Windows-машине. Использовать указанную версию Unity с поддержкой сборки Windows и дождаться восстановления пакетов и первого импорта. Проверить Console на ошибки компиляции и `git status` на неожиданные изменения `ProjectSettings` и файлов пакетов.
2. Открыть `ThreeAreaPrototype`. В Build Profiles выбрать `Windows Standard`, сверить Windows x64, Mono, список сцен и выключенные диагностические параметры. Сохранённый профиль нужно выбрать явно: активная Windows-платформа в редакторе сама по себе этого не подтверждает.
3. При согласованной проверке Player собрать в пустую папку, сохранить ревизию, отчёт сборки, список упакованных путей и контрольную сумму проверяемого комплекта. В Windows проверить запуск из полученной папки, наличие `CREDITS.txt` и `THIRD_PARTY_NOTICES.txt` в `StreamingAssets` и сверить состав по RL-01 / RL-02.

18 ноября через Unity MCP подтверждены целевая платформа, Mono, уровень `PC`, рендер-пайплайн и порядок API в работающем редакторе. Настройки `Windows Standard` и его список сцен сверены по файлу профиля. Сверены 51 прямое закрепление пакетов с lock-файлом и два существующих PE x64 Player с отчётами об успешной сборке. Повторное открытие чистой копии и сборка из неё на согласованной Windows-машине остаются открытыми.

## Поддержка Windows-версии (PR-10)

Для каждого сообщения записать имя или версию скачанного файла, версию Windows, главу и место сбоя, шаги, ожидаемый и фактический результат. Уточнить, повторяется ли сбой после перезапуска игры. Связать сообщение с ревизией по записи RL-06. Для первого сообщения на странице игры достаточно этих сведений. Файлы запрашивать при необходимости через непубличный канал.

| Сбой | Что уточнить | Что повторить в Player |
|---|---|---|
| Сохранение и продолжение | Новая игра или продолжение, последний успешный выход, сообщение главного меню, что пропало после загрузки. При необходимости запросить копии основного и резервного файлов. | Закрыть игру и сохранить обе копии до попыток восстановления. На копиях в отдельном тестовом профиле проверить обычное продолжение и загрузку резерва. Сброс оставлять за явным решением игрока. |
| Мышь и клавиатура | Какая клавиша или кнопка, удерживалась ли ЛКМ, какое меню было открыто, переключалось ли окно, оконный или полноэкранный режим. | Повторить потерю и возврат фокуса, освобождение и повторное нажатие ЛКМ, открытие и закрытие паузы и магазина по QA-01. |
| Просадка FPS или рывки | CPU, GPU, версия драйвера, разрешение и режим экрана, место и действие в момент сбоя. Уточнить, падение постоянное или происходит при действии вроде снятия слоя или автосохранения. | Сверить сцену и параметры с профилем PF-001. Повторить одно и то же действие при одинаковых условиях и записать p95 полного кадра и характер пиков. |

Стандартная папка данных в Windows — `%USERPROFILE%\AppData\LocalLow\Just One More Peel\Just One More Peel`. Прогресс хранится в `three-area-save-v1.json`, резерв в `three-area-save-v1.json.bak`. `Player.log` из этой папки, если он есть, копировать сразу после воспроизведения. Сейвы и полный журнал не нужны в публичном комментарии. [Путь данных Unity](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Application-persistentDataPath.html) · [Путь журналов Unity](https://docs.unity.com/en-us/engine/6000.0/manual/get-started/project-configuration/log-files#player-related-log-locations).

Срочно разбирать потерю прогресса, невозможность запустить игру или закончить смену, блокировку управления после возврата фокуса и зависания, вынуждающие перезапускать игру. Прочие случаи ставить в очередь с шагами и частотой воспроизведения. Для исправления сначала воспроизвести сбой на сообщённой версии, затем повторить тот же сценарий на исправленной и проверить соседний сценарий из таблицы. Если затронут сейв, дополнительно проверить продолжение на копии сохранения из предыдущей версии. [Плейтесты](PLAYTEST.md#этап-4--надёжность-среза) · [Профиль замера](FIRST_SESSION_BASELINE.md#pr-05-паспорт-будущего-player-замера).

## Isolated experiments and local history

- Для обычной очереди достаточно текущего рабочего дерева. Для независимой пробы с отдельными изменениями используй отдельный `git worktree`, если затраты на отдельный импорт Unity оправданы. Перед запуском проверки сверяй путь проекта и экземпляр Unity MCP. Для параллельных проб заранее разделяй файлы и сцены.
- Перед изменениями проверь локальный статус. Если задача включает коммит, фиксируй проверенный результат отдельным коммитом по задаче. Чужие изменения и незавершённые пробы сохраняй отдельно.
- Для разбора чужой игры сформулируй один вопрос и запиши наблюдение по доступному игровому процессу, затем проверь свою реализацию. Если нужен пример кода, используй открытый исходник с проверенной совместимой лицензией. Для двери сравни обозначение обязательной зоны, для отрыва — момент визуального и звукового отклика.

## Codex skills and models

- **GPT-6.2 Sol, Medium** — по умолчанию для обычного плейтеста, локальной правки UI и примерки готового ассета. Используй известный сценарий, узкую проверку и Console.
- **GPT-6.2 Sol, High** — для связанных правил, где нужны явные проверки совместимости, например распределения слоёв и восстановления коллекции из старых сохранений.
- **GPT-6.2 Astra, High** — когда узкая попытка на Sol выявила конкретный нерешённый вопрос или цена ошибки высока и решение затрагивает несколько систем. Укажи вопрос, имеющиеся данные и какую часть задачи передаёшь Astra. Сложную постановку или интеграцию модели оценивай по тому же критерию.
- Сначала сужай сценарий, контекст и объём вывода до нужных для решения. Исторические назначения в закрытых карточках сохраняются.

- **Unity.** Start with the project skills in `.agents/skills` and use Unity MCP for Editor work. Read only the relevant references. Check `ProjectSettings/ProjectVersion.txt` and `Packages/packages-lock.json` when a recipe depends on a particular Editor or package version.
- **Gameplay rules and design decisions.** Use [domain-modeling](../.agents/skills/domain-modeling/SKILL.md) when a rule spans peeling, progression, economy, collections or saves, as in MAIN-011 and HUB-102. Use [grill-with-docs](../.agents/skills/grill-with-docs/SKILL.md) to resolve a concrete trade-off against the plan and playtest evidence, such as where paper is handed in after MAIN-012.
- **Store text.** Use [copywriting](../.agents/skills/copywriting/SKILL.md) when revising the RU/EN itch.io description or download instructions for the target release.
- **Ponytail.** Use `$ponytail:ponytail-review` for a focused pass on unnecessary complexity. Automatic session guidance requires trusted Ponytail hooks. In the CLI, review the entries under `/hooks` and open a new chat after authorizing them. [Ponytail](https://github.com/DietrichGebert/ponytail#install)
- **Agent Skills.** Select the applicable plugin skill from Codex's skill picker. Use `agent-skills:incremental-implementation` to slice a multi-file change, `agent-skills:code-review-and-quality` for diff review, and `agent-skills:debugging-and-error-recovery` for diagnostics alongside the project debugging skill when needed. Use `agent-skills:test-driven-development` when a new test is warranted. Start with a failing regression, make the smallest fix, then rerun the focused test. Follow the project's verification scope. [Agent Skills for Codex](https://github.com/addyosmani/agent-skills/blob/main/docs/codex-setup.md)
- **Graphify.** Use `$graphify` for C# dependency and impact analysis. Follow the local PowerShell workflow in the project skill, and verify relevant results against source and Unity serialization.
- **Models.** Use the model and reasoning effort already selected in the session unless the owner requests a change. If asked for a recommendation, check the models available in the current Codex client and choose based on the task. For a small documentation edit, favor lower latency. For multi-file implementation or difficult debugging, consider higher reasoning effort if available. Keep model IDs out of Unity skill recipes. OmniRoute requires separate authorization before routing Codex requests.
